namespace QuickAttach.ViewModels;

public class Project : ObservableObject
{
    public string ProjectName
    {
        get;
    }

    public string Path
    {
        get;
    }


    public bool Run
    {
        get => _run;
        set
        {
            SetProperty(ref _run, value);
            if (!value)
            {
                SetProperty(ref _attach, false, nameof(Attach));
            }
        }
    }

    public bool Attach
    {
        get => _attach;
        set
        {
            SetProperty(ref _attach, value);
            if (value)
            {
                SetProperty(ref _run, true, nameof(Run));
            }
        }
    }

    public SolidColorBrush Brush
    {
        get;
    }

    public Project(string projectName, string path, Color color)
    {
        ProjectName = projectName;
        Path = path;
        Brush = new SolidColorBrush(color);
    }

    public Project(string projectName, string path) : this(projectName, path, Colors.Transparent)
    {
    }

    private bool _attach;
    private bool _run;
}