using System.Diagnostics;
using Aprint.Features.Settings;

namespace Aprint.Features.About;

internal sealed class AboutForm : Form
{
    private const string LinkedInUrl = "https://www.linkedin.com/in/adrianorsilva/";
    private readonly ToolTip _linkToolTip = new();

    public AboutForm()
    {
        Text = AppLocalization.Translate("Tray.AboutTitle");
        ClientSize = new Size(440, 270);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9F);
        Icon = Program.AppIcon;
        AcceptButton = CreateCloseButton();
        CancelButton = (Button)AcceptButton;

        Controls.Add(CreateTitleLabel());
        Controls.Add(CreateVersionLabel());
        Controls.Add(CreateDescriptionLabel());
        Controls.Add(CreateAuthorLabel());
        var linkedInLink = CreateLinkedInLink();
        _linkToolTip.SetToolTip(linkedInLink, AppLocalization.Translate("Tray.AboutLinkedInToolTip"));
        Controls.Add(linkedInLink);
        Controls.Add((Control)AcceptButton);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _linkToolTip.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Label CreateTitleLabel() => new()
    {
        AutoSize = false,
        Bounds = new Rectangle(28, 24, 384, 34),
        Font = new Font("Segoe UI", 21F, FontStyle.Bold),
        Text = "Aprint",
    };

    private static Label CreateVersionLabel() => new()
    {
        AutoSize = false,
        Bounds = new Rectangle(30, 62, 380, 20),
        ForeColor = SystemColors.GrayText,
        Text = string.Format(AppLocalization.Translate("Tray.AboutVersion"),
            System.Windows.Forms.Application.ProductVersion),
    };

    private static Label CreateDescriptionLabel() => new()
    {
        AutoSize = false,
        Bounds = new Rectangle(30, 89, 380, 80),
        Text = AppLocalization.Translate("Tray.AboutText"),
    };

    private static Label CreateAuthorLabel() => new()
    {
        AutoSize = false,
        Bounds = new Rectangle(30, 173, 380, 20),
        Text = AppLocalization.Translate("Tray.AboutAuthor"),
    };

    private static LinkLabel CreateLinkedInLink()
    {
        var link = new LinkLabel
        {
            AutoSize = false,
            Bounds = new Rectangle(30, 196, 380, 22),
            Text = AppLocalization.Translate("Tray.AboutLinkedIn"),
        };
        link.LinkClicked += (_, _) => Process.Start(new ProcessStartInfo(LinkedInUrl) { UseShellExecute = true });
        return link;
    }

    private static Button CreateCloseButton() => new()
    {
        Bounds = new Rectangle(338, 226, 88, 30),
        Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        Text = AppLocalization.Translate("Tray.AboutClose"),
        UseVisualStyleBackColor = true,
    };
}