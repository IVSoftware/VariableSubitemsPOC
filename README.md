## Variable Subitems POC

As I understand it, your question has a UI component because the card shown in the `CollectionView` will need to accomodate a variable number of tasks. There also seems to be a question about the 'glue' that binds the tasks to the master card with the end result to look something like this. 


[![Nested Subitems collection and variable height cards][1]][1]


  [1]: https://i.stack.imgur.com/g76Kn.png

___

To simplify the database interactions in this broad overview, this code sample uses the **sqlite-pcl-net** package and we'll work with an in-memory database. The MainPage instantiates it and then loads up with `SubItem` tasks where the `DateTime` value falls on various days. It also listens for the Card item to have its binding context set by being added to the `CollectionView` and when that happens it calls into the view model of the individual card so that a database query can be made for tasks with a matching date.

You specifically asked about queries using `Id`, and both the `Card` and `Subitems` do have an `Id` to serve as the unique primary key.However, in testing this answer it seemed to be a better fit to make the queries based on matching _dates_ since you say you want to "list today's task notifications." 

```
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BuildDemoDatabase();
    }

    /// <summary>
    /// An in-memory database to demonstrate ID queries.
    /// </summary>
    internal static SQLiteConnection Database { get; } = new SQLiteConnection(":memory:");

    private void BuildDemoDatabase()
    {
        Database.CreateTable<Card>();
        Database.CreateTable<SubItem>();
        var dt = DateTime.Now;
        Database.InsertAll(new[] 
        {
            new SubItem { DateTime = dt, Description = "Study Econ" },
            new SubItem { DateTime = dt, Description = "PUBG" },
            new SubItem { DateTime = dt, Description = "Meditate for 10 minutes" },
        });
        dt = dt.AddDays(1);
        Database.InsertAll(new[]
        {
            new SubItem { DateTime = dt, Description = "Go for a morning run" },
            new SubItem { DateTime = dt, Description = "Prepare breakfast" }
        });
        dt = dt.AddDays(1);
        Database.InsertAll(new[]
        { 
            new SubItem { DateTime = dt, Description = "Call a friend" },
            new SubItem { DateTime = dt, Description = "Sort emails" },
            new SubItem { DateTime = dt, Description = "Plan the week ahead" },
            new SubItem { DateTime = dt, Description = "Walk Jeremy's dog" },
            new SubItem { DateTime = dt, Description = "Write in journal" },
        });
    }

    private void onBindingContextChanged(object sender, EventArgs e)
    {
        if(sender is View view && view.BindingContext is Card card)
        {
            card.RefreshCommand.Execute(null);
        }
    }
}
```

Where:

```
class SubItem : INotifyPropertyChanged
{
    [PrimaryKey]
    public string Id { get; set; } = $"{Guid.NewGuid()}";
    public DateTime DateTime { get; set; }
    public string Description
    {
        get => _description;
        set
        {
            if (!Equals(_description, value))
            {
                _description = value;
                OnPropertyChanged();
            }
        }
    }
    string _description = string.Empty;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public override string ToString() => Description ?? string.Empty;
}
```
___

#### Card ViewModel

The `RefreshCommand` instructs the card to make a query to the database to collect sub items where the day matches.

```
class Card : INotifyPropertyChanged
{
    public Card(DateTime dt) : this() => DateTime = dt;
    public Card()
    {
        RefreshCommand = new Command(OnRefresh);
    }
    public ICommand RefreshCommand { get; private set; }
    private void OnRefresh(object o)
    {
        SubItems.Clear();
        foreach (var subItem in Database.Query<SubItem>(DateTime.ToDateOnlyQuery(nameof(SubItem))))
        {
            SubItems.Add(subItem);
        }
    }

    [PrimaryKey]
    public string Id { get; set; } = $"{Guid.NewGuid()}";
    public string? Description => 
        DateTime.Equals(DateTime.Today) ? "Today" :
        DateTime.Equals(DateTime.Today.AddDays(1)) ? "Tomorrow" :
        DateTime.DayOfWeek.ToString();

    public ObservableCollection<SubItem> SubItems { get; } = new ObservableCollection<SubItem>();

    public DateTime DateTime { get; set; }

    protected virtual void OnPropertyChanged([CallerMemberName]string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public event PropertyChangedEventHandler? PropertyChanged;
}
```

___

##### Extension

The database query uses this extension to extract the DateOnly component from the `DateTime` which is stored as ticks.

```
static partial class Extensions
{
    public static string ToDateOnlyQuery(this DateTime date, string table) =>
        $"SELECT * FROM {table} WHERE DateTime >= {date.Date.Ticks} AND DateTime < {date.Date.AddDays(1).Ticks}";
}
```

___

Finally, the main page view model populates the `ItemSource` of the `CollectionView` with seven days' worth of cards.

```
class MainPageBindingContext
{
    private async Task ExecAddDynamicSubitemsTest()
    {
        const int TEST_CARD_INDEX = 3;
        for (int i = 1; i <= 4; i++)
        {
            await Task.Delay(1000); 
            var newTaskDescription = $"Dynamic Task {i}";
            Items[TEST_CARD_INDEX].SubItems.Add(new SubItem { Description = newTaskDescription });
        }
    }
    public ObservableCollection<Card> Items { get; } = new ObservableCollection<Card>
    {
        new Card(DateTime.Today),
        new Card(DateTime.Today.AddDays(1)),
        new Card(DateTime.Today.AddDays(2)),
        new Card(DateTime.Today.AddDays(3)),
        new Card(DateTime.Today.AddDays(4)),
        new Card(DateTime.Today.AddDays(5)),
        new Card(DateTime.Today.AddDays(6)),
    };
}
```
