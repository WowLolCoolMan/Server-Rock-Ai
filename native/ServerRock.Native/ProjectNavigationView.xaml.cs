using Microsoft.UI.Xaml.Controls;

namespace ServerRock.Native;

public sealed partial class ProjectNavigationView : UserControl
{
    public StackPanel CodingNavigationSectionControl => CodingNavigationSection;
    public StackPanel ProjectListControl => ProjectList;
    public TextBlock CodeFilesHeaderControl => CodeFilesHeader;
    public StackPanel ProjectFileListControl => ProjectFileList;

    public ProjectNavigationView()
    {
        InitializeComponent();
    }
}
