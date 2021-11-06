using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Artemis.UI.Avalonia.Screens.Device.Views
{
    public partial class InputMappingsTabView : UserControl
    {
        public InputMappingsTabView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
