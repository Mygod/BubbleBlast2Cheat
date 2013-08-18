namespace Mygod.BubbleBlast2.Cheat
{
    public partial class TextWindow
    {
        public TextWindow(string title, string properties)
        {
            InitializeComponent();
            Title = title;
            TextBox.Text = properties;
        }
    }
}
