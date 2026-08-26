using System.ComponentModel;
using System.Windows;
using InstaladorGuis.Models;
using InstaladorGuis.ViewModels;

namespace InstaladorGuis.Tests.ViewModels;

public class GuiVmTests
{
    [Fact]
    public void Chip_ReflectsPackageType()
    {
        var paq = new GuiVm { EsPrendas = false };
        var prc = new GuiVm { EsPrendas = true };
        Assert.Equal("PAQ", paq.Chip);
        Assert.Equal("PRC", prc.Chip);
    }

    [Fact]
    public void StatusLabel_ShowsInstalledAndChecking()
    {
        var vm = new GuiVm();
        Assert.Equal("", vm.StatusLabel);

        vm.Status = GuiStatus.Installed;
        Assert.Equal("instalada", vm.StatusLabel);
        Assert.Equal(Visibility.Visible, vm.StatusLabelVisible);

        vm.Status = GuiStatus.Checking;
        Assert.Equal("procesando…", vm.StatusLabel);

        vm.Status = GuiStatus.NotInstalled;
        Assert.Equal("", vm.StatusLabel);
        Assert.Equal(Visibility.Collapsed, vm.StatusLabelVisible);
    }

    [Fact]
    public void IsChecked_RaisesPropertyChanged()
    {
        var vm = new GuiVm();
        string? changed = null;
        vm.PropertyChanged += (_, e) => changed = e.PropertyName;
        vm.IsChecked = true;
        Assert.Equal(nameof(GuiVm.IsChecked), changed);
    }

    [Fact]
    public void Status_RaisesDependentProperties()
    {
        var vm = new GuiVm();
        var names = new HashSet<string>();
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != null) names.Add(e.PropertyName);
        };

        vm.Status = GuiStatus.Installed;

        Assert.Contains(nameof(GuiVm.Status), names);
        Assert.Contains(nameof(GuiVm.CardBackground), names);
        Assert.Contains(nameof(GuiVm.AccentBrush), names);
        Assert.Contains(nameof(GuiVm.StatusLabel), names);
    }
}
