using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Shapes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using ReactiveUI;

namespace Artemis.UI.Screens.VisualScripting;

public class CableView : ReactiveUserControl<CableViewModel>
{
    private const double CABLE_OFFSET = 24 * 4;
    private readonly Path _cablePath;

    public CableView()
    {
        InitializeComponent();
        _cablePath = this.Get<Path>("CablePath");

        // Not using bindings here to avoid a warnings
        this.WhenActivated(d =>
        {
            ViewModel.WhenAnyValue(vm => vm.FromPoint).Subscribe(_ => Update()).DisposeWith(d);
            ViewModel.WhenAnyValue(vm => vm.ToPoint).Subscribe(_ => Update()).DisposeWith(d);
            Update();
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void Update()
    {
        // Workaround for https://github.com/AvaloniaUI/Avalonia/issues/4748
        _cablePath.Margin = _cablePath.Margin != new Thickness(0, 0, 0, 0) ? new Thickness(0, 0, 0, 0) : new Thickness(1, 1, 0, 0);

        PathFigure pathFigure = ((PathGeometry) _cablePath.Data).Figures.First();
        BezierSegment segment = (BezierSegment) pathFigure.Segments!.First();
        pathFigure.StartPoint = ViewModel!.FromPoint;
        segment.Point1 = new Point(ViewModel.FromPoint.X + CABLE_OFFSET, ViewModel.FromPoint.Y);
        segment.Point2 = new Point(ViewModel.ToPoint.X - CABLE_OFFSET, ViewModel.ToPoint.Y);
        segment.Point3 = new Point(ViewModel.ToPoint.X, ViewModel.ToPoint.Y);
    }
}