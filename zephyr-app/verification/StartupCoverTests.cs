using System;
using System.Drawing;
using System.Windows.Forms;
using SkinFusion;

internal static class StartupCoverTests
{
    [STAThread]
    static int Main()
    {
        using (var form = new Form { ClientSize = new Size(480, 710), ShowInTaskbar = false, Opacity = 0 })
        {
            var menu = new MenuStrip();
            menu.Items.Add("Preferences");
            var start = new Button { Text = "Start", Location = new Point(30, 60) };
            int clicks = 0;
            start.Click += delegate { clicks++; };
            form.Controls.Add(start);
            form.Controls.Add(menu);
            form.Show();
            var wide = StartupPanel.LogoBounds(new Size(480, 710), new Size(400, 200), 96);
            var tall = StartupPanel.LogoBounds(new Size(480, 710), new Size(200, 400), 96);
            if (wide.Width / wide.Height != 2 || tall.Height / tall.Width != 2) throw new Exception("Logo aspect ratio changed.");
            if (wide.X + wide.Width / 2 != 240 || wide.Y + wide.Height / 2 != 355) throw new Exception("Logo is not centered.");
            Panel cover = StartupCover.Install(form);
            form.PerformLayout();
            if (cover.Bounds != form.ClientRectangle) throw new Exception("Menu bar is not covered.");
            if (!start.Enabled || !start.Visible) throw new Exception("Original controls were changed.");
            start.PerformClick();
            if (clicks != 1) throw new Exception("Original click handler stopped working.");
            form.ClientSize = new Size(540, 760);
            form.PerformLayout();
            if (cover.Bounds != form.ClientRectangle) throw new Exception("Resized window is not fully covered.");
            using (var image = new Bitmap(cover.Width, cover.Height))
            {
                cover.DrawToBitmap(image, cover.ClientRectangle);
                if (image.GetPixel(5, 5).ToArgb() != Color.Black.ToArgb()) throw new Exception("Startup is not black.");
                bool visibleLogo = false;
                for (int x = cover.Width / 2 - 60; x < cover.Width / 2 + 60; x++)
                    for (int y = cover.Height / 2 - 60; y < cover.Height / 2 + 60; y++)
                        if (image.GetPixel(x, y).ToArgb() != Color.Black.ToArgb()) visibleLogo = true;
                if (!visibleLogo) throw new Exception("Centered logo is missing.");
            }
            StartupCover.Remove(form);
            if (form.Controls.Contains(cover) || !cover.IsDisposed) throw new Exception("Startup cover was not disposed.");
            if (!menu.Visible || !start.Enabled) throw new Exception("Original menu or controls were lost.");
            form.Close();
        }
        Console.WriteLine("Startup cover checks passed: full menu coverage, centered logo, resizing and original click handler.");
        return 0;
    }
}
