using IVSoftware.Portable.Disposable;
using SQLite;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using static IVSoftware.Portable.Disposable.Clients;
using static VariableSubitemsPOC.MainPageBindingContext; // Make Database visible as static

namespace VariableSubitemsPOC
{
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
        protected override bool OnBackButtonPressed()
        {
            switch (BindingContext.OnePageState)
            {
                case OnePageState.Main: break;
                case OnePageState.Detail: BindingContext.OnePageState = OnePageState.Main; break;
            }
            return true;
        }
    }
    enum OnePageState { Main, Detail } // Type is visible in xaml as x:Static
    class MainPageBindingContext : INotifyPropertyChanged
    {
        public MainPageBindingContext()
        {
            RefreshCommand = new Command(onRefresh);
            if (Path.GetDirectoryName(DatabasePath) is string valid)
            {
                Directory.CreateDirectory(valid);
            }
            OnePageStateRequestArgs.OnePageStateRequest += (sender, e) =>
            {
                OnePageState = e.OnePageState;
                if(sender is TaskItem taskItem)
                {
                    CurrentTask = taskItem;
                }
            }; 
// To reset, change to
// #if !RESET_DATABASE
#if !RESET_DATABASE 
            File.Delete(DatabasePath);
#endif
            if (!File.Exists(DatabasePath)) 
            {
                BuildDemoDatabase();
            }
        } 
        // This is to suppress sqlite updates while the object is loading.
        public static DisposableHost DHostLoading { get; } = new DisposableHost();

        public readonly static string DatabasePath =
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                nameof(VariableSubitemsPOC),
                "tasks.db");

        public TaskItem? CurrentTask
        {
            get => _currentTask;
            set
            {
                if (!Equals(_currentTask, value))
                {
                    _currentTask = value;
                    OnPropertyChanged();
                }
            }
        }
        TaskItem? _currentTask = null;

        public OnePageState OnePageState
        {
            get => _onePageState;
            set
            {
                if (!Equals(_onePageState, value))
                {
                    _onePageState = value;
                    OnPropertyChanged();
                }
            }
        }
        OnePageState _onePageState = OnePageState.Main;

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
#if false && DEBUG
        private async Task ExecAddDynamicSubitemsTest()
        {
            const int TEST_CARD_INDEX = 3;
            for (int i = 1; i <= 4; i++)
            {
                await Task.Delay(1000); 
                var newTaskDescription = $"Dynamic Task {i}";
                Items[TEST_CARD_INDEX].SubItems.Add(new TaskItem { Description = newTaskDescription });
            }
        }
#endif
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
        private void BuildDemoDatabase()
        {
            using (var database = new SQLiteConnection(DatabasePath))
            {
                database.CreateTable<Card>();
                database.CreateTable<TaskItem>();
                database.CreateTable<DetailItem>();

                var builder = new List<object>();
                string parentId;
                TaskItem taskItem;
                var dt = DateTime.Now;

                taskItem = new TaskItem { DateTime = dt, Description = "Weekly grocery shopping" };
                parentId = taskItem.Id;
                builder.Add(taskItem);
                builder.Add(new DetailItem { ParentId = parentId, Description = "List groceries needed" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Check coupons/sales" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Visit supermarket" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Buy fruits, veggies, meat, dairy" });

                taskItem = new TaskItem { DateTime = dt, Description = "Study for exams" };
                parentId = taskItem.Id;
                builder.Add(taskItem);
                builder.Add(new DetailItem { ParentId = parentId, Description = "Review math notes" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Solve textbook problems" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Study scientific methods" });

                dt = dt.AddDays(1);
                taskItem = new TaskItem { DateTime = dt, Description = "Deep clean house" };
                parentId = taskItem.Id;
                builder.Add(taskItem);
                builder.Add(new DetailItem { ParentId = parentId, Description = "Vacuum carpets/rugs" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Dust and clean windows" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Mop kitchen/bathroom" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Organize living room" });

                taskItem = new TaskItem { DateTime = dt, Description = "Morning yoga routine" };
                parentId = taskItem.Id;
                builder.Add(taskItem);
                builder.Add(new DetailItem { ParentId = parentId, Description = "Prepare yoga mat" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Start with stretching" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Follow online class" });

                taskItem = new TaskItem { DateTime = dt, Description = "Plan healthy meals" };
                parentId = taskItem.Id;
                builder.Add(taskItem);
                builder.Add(new DetailItem { ParentId = parentId, Description = "Research recipes" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "List ingredients" });
                builder.Add(new DetailItem { ParentId = parentId, Description = "Create meal schedule" });

                dt = dt.AddDays(1);
                var taskOrganizeOffice = new TaskItem { DateTime = dt, Description = "Organize home office" };
                parentId = taskOrganizeOffice.Id;
                builder.Add(taskOrganizeOffice);

                database.InsertAll(builder);
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler? PropertyChanged;
    }
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

    class TaskItem : INotifyPropertyChanged
    {
        public TaskItem()
        {
            LabelTappedCommand = new Command(OnLabelTapped);
        }
        public ICommand LabelTappedCommand { get; private set; }
        private void OnLabelTapped(object o)
        {
            using (var database = new SQLiteConnection(DatabasePath, SQLiteOpenFlags.ReadOnly))
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
    class OnePageStateRequestArgs : EventArgs
    {
        public OnePageState OnePageState { get; }

        public OnePageStateRequestArgs(OnePageState onePageState)
        {
            OnePageState = onePageState;
        }
        public void FireSelf(object sender) => 
            OnePageStateRequest?.Invoke(sender, this);
        public static event EventHandler<OnePageStateRequestArgs>? OnePageStateRequest;
    }

    static partial class Extensions
    {
        public static string ToDateOnlyQuery(this DateTime date, string table) =>
            $"SELECT * FROM {table} WHERE DateTime >= {date.Date.Ticks} AND DateTime < {date.Date.AddDays(1).Ticks}";
    }
}
