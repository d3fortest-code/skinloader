namespace SkinEngine.Sandbox;

using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using SkinEngine.Sandbox.Demos;

public class DemoSelectorForm : Form
{
    private readonly Label _skinPathLabel;
    private readonly Button _loadButton;
    private readonly RadioButton _scale1x, _scale2x, _scale3x, _scale4x;

    public DemoSelectorForm()
    {
        Text = "SkinEngine Sandbox";
        ClientSize = new Size(400, 520);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var skinGroup = new GroupBox
        {
            Text = "Current Skin",
            Location = new Point(10, 10),
            Size = new Size(380, 60),
        };

        _skinPathLabel = new Label
        {
            Text = Program.SkinPath ?? "(no skin loaded)",
            Location = new Point(8, 22),
            Size = new Size(260, 20),
            AutoEllipsis = true,
            ForeColor = Program.SkinPath is not null ? SystemColors.ControlText : SystemColors.GrayText,
        };

        _loadButton = new Button
        {
            Text = "Load...",
            Location = new Point(280, 19),
            Size = new Size(85, 26),
        };
        _loadButton.Click += LoadButton_Click;

        skinGroup.Controls.Add(_skinPathLabel);
        skinGroup.Controls.Add(_loadButton);
        Controls.Add(skinGroup);

        var scaleGroup = new GroupBox
        {
            Text = "Scale",
            Location = new Point(10, 80),
            Size = new Size(380, 50),
        };

        _scale1x = new RadioButton { Text = "1x", Location = new Point(12, 20), Size = new Size(50, 20), Checked = Program.ScaleFactor == 1 };
        _scale2x = new RadioButton { Text = "2x", Location = new Point(80, 20), Size = new Size(50, 20), Checked = Program.ScaleFactor == 2 };
        _scale3x = new RadioButton { Text = "3x", Location = new Point(148, 20), Size = new Size(50, 20), Checked = Program.ScaleFactor == 3 };
        _scale4x = new RadioButton { Text = "4x", Location = new Point(216, 20), Size = new Size(50, 20), Checked = Program.ScaleFactor == 4 };

        _scale1x.CheckedChanged += (_, _) => { if (_scale1x.Checked) Program.ScaleFactor = 1; };
        _scale2x.CheckedChanged += (_, _) => { if (_scale2x.Checked) Program.ScaleFactor = 2; };
        _scale3x.CheckedChanged += (_, _) => { if (_scale3x.Checked) Program.ScaleFactor = 3; };
        _scale4x.CheckedChanged += (_, _) => { if (_scale4x.Checked) Program.ScaleFactor = 4; };

        scaleGroup.Controls.AddRange([_scale1x, _scale2x, _scale3x, _scale4x]);
        Controls.Add(scaleGroup);

        var demos = new (string Name, Func<Form> Factory)[]
        {
            ("Button", () => new ButtonDemo()),
            ("ToggleButton", () => new ToggleButtonDemo()),
            ("Slider", () => new SliderDemo()),
            ("Label", () => new LabelDemo()),
            ("Indicator", () => new IndicatorDemo()),
            ("ProgressBar", () => new ProgressBarDemo()),
            ("Visualizer", () => new VisualizerDemo()),
        };

        int y = 150;
        foreach (var (name, factory) in demos)
        {
            var btn = new Button
            {
                Text = name,
                Location = new Point(10, y),
                Size = new Size(380, 30),
                Tag = factory
            };
            btn.Click += (s, e) =>
            {
                if (btn.Tag is Func<Form> f)
                {
                    using var demo = f();
                    demo.ShowDialog(this);
                }
            };
            Controls.Add(btn);
            y += 40;
        }
    }

    private void LoadButton_Click(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Select Winamp Skin",
            Filter = "Winamp Skins (*.wsz)|*.wsz|All Files (*.*)|*.*",
        };

        if (Program.SkinPath is not null)
        {
            dlg.InitialDirectory = Path.GetDirectoryName(Program.SkinPath);
            dlg.FileName = Path.GetFileName(Program.SkinPath);
        }

        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            Program.SkinPath = dlg.FileName;
            Program.SaveLastSkinPath(dlg.FileName);
            _skinPathLabel.Text = dlg.FileName;
            _skinPathLabel.ForeColor = SystemColors.ControlText;
            SkinEngine.Core.AppLogger.Info($"[Sandbox] Skin loaded: {dlg.FileName}");
        }
    }
}
