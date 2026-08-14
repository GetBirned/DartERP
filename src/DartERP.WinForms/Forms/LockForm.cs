using DartERP.Application.Services;
using DartERP.Core.Models;
using DartERP.WinForms.Controls;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

/// <summary>
/// Re-authenticates the already-signed-in user without ending their session
/// — the "lock screen" half of the lock+logout pair. Session state
/// (CurrentUserContext) is never touched here; a correct password just
/// closes this modal and MainForm carries on exactly where it was.
/// </summary>
public class LockForm : Form
{
    private readonly UserService _userService;
    private readonly User _user;
    private readonly TextBox _passwordBox = new TextBox().StyleAsInput();
    private readonly Label _errorLabel;

    public LockForm(UserService userService, User user)
    {
        _userService = userService;
        _user = user;

        Text = "Locked";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        TopMost = true;
        ClientSize = new Size(360, 320);
        BackColor = Theme.CardBackground;

        _passwordBox.UseSystemPasswordChar = true;
        _passwordBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode != Keys.Enter)
                return;
            e.SuppressKeyPress = true;
            await UnlockAsync();
        };

        var avatar = new Avatar { Width = 72, Height = 72, Location = new Point((300 - 72) / 2, 0) };
        avatar.SetUser(user.DisplayName, user.Username, ProfilePictureStore.Load(user.ProfilePicturePath));
        // Wrapped in a fixed-width host so the avatar can be centered with a
        // one-time Location set — putting it straight into the FlowLayoutPanel
        // below would fight that panel's own automatic positioning on every
        // relayout and snap it back to the left edge.
        var avatarHost = new Panel { Width = 300, Height = 72 };
        avatarHost.Controls.Add(avatar);

        var nameLabel = new Label
        {
            Text = user.DisplayName,
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Width = 300,
            Height = 26,
        };

        var subLabel = new Label
        {
            Text = "Enter your password to continue",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Width = 300,
            Height = 20,
        };

        _errorLabel = new Label
        {
            Text = string.Empty,
            Font = Theme.FontSmall,
            ForeColor = Theme.DangerRed,
            TextAlign = ContentAlignment.MiddleCenter,
            AutoSize = false,
            Width = 300,
            Height = 20,
        };

        var unlockButton = new Button { Text = "Unlock", Width = 300, Height = 36 }.StyleAsPrimaryButton();
        unlockButton.Click += async (_, _) => await UnlockAsync();

        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Anchor = AnchorStyles.None,
        };
        stack.Controls.Add(avatarHost);
        stack.Controls.Add(new Panel { Height = 12, Width = 300 });
        stack.Controls.Add(nameLabel);
        stack.Controls.Add(subLabel);
        stack.Controls.Add(new Panel { Height = 12, Width = 300 });
        _passwordBox.Width = 300;
        _passwordBox.Height = 30;
        stack.Controls.Add(_passwordBox);
        stack.Controls.Add(new Panel { Height = 4, Width = 300 });
        stack.Controls.Add(_errorLabel);
        stack.Controls.Add(unlockButton);

        Controls.Add(stack);
        Load += (_, _) =>
        {
            stack.Location = new Point((ClientSize.Width - stack.Width) / 2, (ClientSize.Height - stack.Height) / 2);
        };
    }

    private async Task UnlockAsync()
    {
        _errorLabel.Text = string.Empty;
        var authenticated = await _userService.AuthenticateAsync(_user.Username, _passwordBox.Text);
        if (authenticated is null)
        {
            _errorLabel.Text = "Incorrect password.";
            _passwordBox.SelectAll();
            _passwordBox.Focus();
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}
