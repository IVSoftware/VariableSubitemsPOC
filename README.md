## Variable Subitems POC

As I understand it, you are displaying some daily tasks in a collection view, and want to query the database for task details based on some event (for example, tapping the task line). There are many ways to do this, but I hope to, as you say, point you in the right direction. Two things to know in advance: For **SQLite** I'm specifically using the **sqlite-pcl-net** nuget for this sample. Also, to try and limit the code I post here, you can browse or clone a full sample on [GitHub](https://github.com/IVSoftware/VariableSubitemsPOC);

___

[Placeholder]

___

In this version, there are two tables in the SQLite database, corresponding to a `TaskItem` record class for the parent item and a `DetailItem` record class for its subitems. For the task item, the data template that will host it in the xaml will attach a `TapGestureRecognizer` to the label displaying the task description, which will call the `LabelTappedCommand` in the view model. In that method, the database will be queried for detail items with a `ParentId` value equal to the `TaskItem` that has been tapped. Once the detail items are retrieved, one approach would be to use shell navigation to display the details in an entirely different view, or alternatively stay on the main page and use `OnePage` navigation to hide the task grid and show the details grid as is the case here.

```
class TaskItem : INotifyPropertyChanged
{
    public TaskItem()
    {
        LabelTappedCommand = new Command(OnLabelTapped);
    }
    public ICommand LabelTappedCommand { get; private set; }
    private void OnLabelTapped(object o)
    {
        using (var database = new SQLiteConnection(DatabasePath))

        using(DHostLoading.GetToken())
        {
            var sql = $"select * from {nameof(DetailItem)} where {nameof(DetailItem.ParentId)} = '{Id}'";
            Details.Clear();
            foreach (var detail in database.Query<DetailItem>(sql))
            {
                Details.Add(detail);
            }
            new OnePageStateRequestArgs(OnePageState.Detail).FireSelf(this);
        }
    }
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

    public ObservableCollection<DetailItem> Details { get; } = 
        new ObservableCollection<DetailItem>();
}
```

___

The `DetailItem` is expecting to be hosted in a data template that has a checkbox to indicate whether the task has been completed, and when the user changes this value it will be committed to the database. A word of caution would be that having the `Done` property directly bound to the database update could be problematic when the property is changing _as a result of a query that is loading it_. In fact, the database is likely to hang. (This kind of circularity is a common issue when loading objects with persisted properties.) To prevent this, this sample uses a reference-counted semaphore that is checked out before the query is made.

```
class DetailItem : INotifyPropertyChanged
{
    [PrimaryKey]
    public string Id { get; set; } = $"{Guid.NewGuid()}";
    public string ParentId { get; set; } = $"{Guid.NewGuid()}";
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

    public bool Done
    {
        get => _done;
        set
        {
            if (!Equals(_done, value))
            {
                _done = value;
                OnPropertyChanged();
                if (DHostLoading.IsZero())
                {
                    using (var database = new SQLiteConnection(DatabasePath))
                    {
                        var sql = $"update {nameof(DetailItem)} set {nameof(DetailItem.Done)} = {Done} where {nameof(Id)} = '{Id}'";
                        database.Execute(sql);
                    }
                }
            }
        }
    }
    bool _done = default;

    public override string ToString() => Description ?? string.Empty;
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}
```

**Main Page**

In the screenshow shown, the top level collection view isn't showing `TaskItem` objects or `DetailItem` object directly. For demonstration purposes, the main view consiste of 7 `Card` objects representing today, tomorrow, and the other five days in the week to come. When the view is refreshed by some event (in this case on `Appearing`) each Card item issues a query to retrieve task items from the dictionary that occur any time during the day represented by the card.

```
class Card : INotifyPropertyChanged
{
    public Card(DateTime dt) : this() => DateTime = dt;
    public Card() { }

    [PrimaryKey]
    public string Id { get; set; } = $"{Guid.NewGuid()}";
    public string? Description => 
        DateTime.Equals(DateTime.Today) ? "Today" :
        DateTime.Equals(DateTime.Today.AddDays(1)) ? "Tomorrow" :
        DateTime.DayOfWeek.ToString();

    public ObservableCollection<TaskItem> TaskItems { get; } = new ObservableCollection<TaskItem>();

    public DateTime DateTime { get; set; }

    protected virtual void OnPropertyChanged([CallerMemberName]string? propertyName = null) => 
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    public event PropertyChangedEventHandler? PropertyChanged;
}
```
___

```
public partial class MainPage : ContentPage
{
    public MainPage() => InitializeComponent();

    new MainPageBindingContext BindingContext =>
        (MainPageBindingContext)base.BindingContext;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BindingContext.RefreshCommand.Execute(null);
    }
}
```

___

```
class MainPageBindingContext : INotifyPropertyChanged
{
    public ObservableCollection<Card> Days { get; } = new ObservableCollection<Card>
    {
        new Card(DateTime.Today),
        new Card(DateTime.Today.AddDays(1)),
        new Card(DateTime.Today.AddDays(2)),
        new Card(DateTime.Today.AddDays(3)),
        new Card(DateTime.Today.AddDays(4)),
        new Card(DateTime.Today.AddDays(5)),
        new Card(DateTime.Today.AddDays(6)),
    };

    // <PackageReference Include="IVSoftware.Portable.Disposable" Version="1.2.0" />
    // This is to suppress sqlite updates while the object is loading.
    public static DisposableHost DHostLoading { get; } = new DisposableHost();

    public ICommand RefreshCommand { get; private set; }
    private void onRefresh(object o)
    {
        using (var database = new SQLiteConnection(DatabasePath))
        {
            foreach (var day in Days)
            {
                var sql = day.DateTime.ToDateOnlyQuery(nameof(TaskItem));
                foreach (var taskItem in database.Query<TaskItem>(sql))
                {
                    day.TaskItems.Add(taskItem);
                }
            }
        }
    }
    .
    .
    .
}
static partial class Extensions
{
    public static string ToDateOnlyQuery(this DateTime date, string table) =>
        $"SELECT * FROM {table} WHERE DateTime >= {date.Date.Ticks} AND DateTime < {date.Date.AddDays(1).Ticks}";
}
```

___

**Xaml**

```
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local ="clr-namespace:VariableSubitemsPOC"
             x:Class="VariableSubitemsPOC.MainPage"
             Shell.NavBarIsVisible="{
                Binding OnePageState, 
                Converter={StaticResource EnumToBoolConverter}, 
                ConverterParameter={x:Static local:OnePageState.Main}}">
    <ContentPage.BindingContext>
        <local:MainPageBindingContext />
    </ContentPage.BindingContext>
    <ContentPage.Resources>
        <ResourceDictionary>
            <local:EnumToBoolConverter x:Key="EnumToBoolConverter"/>
        </ResourceDictionary>
    </ContentPage.Resources>
    <Grid>
        <Grid
            IsVisible="{
                Binding OnePageState, 
                Converter={StaticResource EnumToBoolConverter}, 
                ConverterParameter={x:Static local:OnePageState.Main}}"
            Padding="30,0" 
            RowDefinitions="70, *">
            <Image
            Source="dotnet_bot.png"
            HeightRequest="70"
            Aspect="AspectFit"
            VerticalOptions="Center"
            SemanticProperties.Description="dot net bot in a race car number eight" />
            <CollectionView 
            Grid.Row="1"
            ItemsSource="{Binding Days}" 
            BackgroundColor="Azure">
                <CollectionView.ItemTemplate>
                    <DataTemplate>
                        <Frame
                            Padding="10"
                            Margin="5"
                            BorderColor="Gray"
                            CornerRadius="10"
                            HasShadow="True">
                            <StackLayout>
                                <Label 
                                    Text="{Binding Description}" 
                                    FontAttributes="Bold"
                                    FontSize="Medium"
                                    HorizontalOptions="Fill"
                                    HorizontalTextAlignment="Start"
                                    VerticalTextAlignment="Center"/>
                                <StackLayout>
                                    <StackLayout 
                                        BindableLayout.ItemsSource="{Binding TaskItems}">
                                        <BindableLayout.ItemTemplate>
                                            <DataTemplate>
                                                <Label 
                                                Text="{Binding Description}" 
                                                FontSize="Small" 
                                                Margin="2,2">
                                                    <Label.GestureRecognizers>
                                                        <TapGestureRecognizer Command="{Binding LabelTappedCommand}"/>
                                                    </Label.GestureRecognizers>
                                                </Label>
                                            </DataTemplate>
                                        </BindableLayout.ItemTemplate>
                                    </StackLayout>
                                </StackLayout>
                            </StackLayout>
                        </Frame>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </Grid>
        .
        .
        .

    </Grid>
</ContentPage>
```
