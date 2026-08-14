using DartERP.Application;
using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.WinForms.Controls;
using DartERP.WinForms.Local;
using DartERP.WinForms.Styling;

namespace DartERP.WinForms.Forms;

public class ProfileForm : Form
{
    private readonly UserService _userService;
    private readonly CurrentUserContext _currentUserContext;

    private readonly Avatar _avatar = new() { Width = 72, Height = 72 };
    private readonly TextBox _usernameBox = new TextBox().StyleAsInput();
    private readonly TextBox _displayNameBox = new TextBox().StyleAsInput();
    private readonly TextBox _roleBox = new TextBox().StyleAsInput();
    private readonly TextBox _phoneBox = new TextBox().StyleAsInput();
    private readonly TextBox _emailBox = new TextBox().StyleAsInput();
    private readonly Label _profileError;

    private readonly TextBox _currentPasswordBox = new TextBox().StyleAsInput();
    private readonly TextBox _newPasswordBox = new TextBox().StyleAsInput();
    private readonly TextBox _confirmPasswordBox = new TextBox().StyleAsInput();
    private readonly Label _passwordStatus;

    /// <summary>Raised after a successful profile save or picture change, so MainForm can refresh its header without a full shell rebuild.</summary>
    public event EventHandler? ProfileUpdated;

    public ProfileForm(UserService userService, CurrentUserContext currentUserContext)
    {
        _userService = userService;
        _currentUserContext = currentUserContext;
        var user = currentUserContext.CurrentUser!;

        Text = "My Profile";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(480, 660);
        BackColor = Theme.CardBackground;

        _usernameBox.Text = user.Username;
        _usernameBox.Enabled = false;
        _displayNameBox.Text = user.DisplayName;
        _roleBox.Text = user.Role;
        _phoneBox.Text = user.Phone;
        _emailBox.Text = user.Email;
        _avatar.SetUser(user.DisplayName, user.Username, ProfilePictureStore.Load(user.ProfilePicturePath));

        var changePictureButton = new Button { Text = "Change Picture", Width = 140, Height = 30 }.StyleAsSecondaryButton();
        changePictureButton.Click += (_, _) => UploadPicture();

        var pictureRow = new Panel { Dock = DockStyle.Top, Height = 90, Padding = new Padding(24, 16, 24, 0) };
        _avatar.Location = new Point(0, 0);
        changePictureButton.Location = new Point(_avatar.Width + 16, (_avatar.Height - changePictureButton.Height) / 2);
        pictureRow.Controls.Add(_avatar);
        pictureRow.Controls.Add(changePictureButton);

        var profileLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 6,
            Padding = new Padding(24, 12, 24, 0),
            AutoSize = true,
        };
        profileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        profileLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        FormLayoutHelper.AddRow(profileLayout, 0, "Username", _usernameBox);
        FormLayoutHelper.AddRow(profileLayout, 1, "Display Name*", _displayNameBox);
        FormLayoutHelper.AddRow(profileLayout, 2, "Role", _roleBox);
        FormLayoutHelper.AddRow(profileLayout, 3, "Phone", _phoneBox);
        FormLayoutHelper.AddRow(profileLayout, 4, "Email", _emailBox);
        _profileError = FormLayoutHelper.AddValidationLabel(profileLayout, 5);

        var saveProfileButton = new Button { Text = "Save Profile", Width = 140, Height = 32 }.StyleAsPrimaryButton();
        saveProfileButton.Click += async (_, _) => await SaveProfileAsync();
        var saveProfileRow = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(24, 4, 24, 0) };
        saveProfileRow.Controls.Add(saveProfileButton);

        var divider = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = Theme.BorderColor, Margin = new Padding(0, 16, 0, 0) };
        var dividerHost = new Panel { Dock = DockStyle.Top, Height = 24, Padding = new Padding(24, 12, 24, 0) };
        dividerHost.Controls.Add(divider);

        var passwordHeading = new Label
        {
            Text = "Change Password",
            Font = Theme.FontSubheader,
            ForeColor = Theme.TextPrimary,
            Dock = DockStyle.Top,
            Height = 30,
            Padding = new Padding(24, 4, 0, 0),
        };

        _currentPasswordBox.UseSystemPasswordChar = true;
        _newPasswordBox.UseSystemPasswordChar = true;
        _confirmPasswordBox.UseSystemPasswordChar = true;

        var passwordLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 4,
            Padding = new Padding(24, 8, 24, 0),
            AutoSize = true,
        };
        passwordLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        passwordLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        FormLayoutHelper.AddRow(passwordLayout, 0, "Current Password", _currentPasswordBox);
        FormLayoutHelper.AddRow(passwordLayout, 1, "New Password", _newPasswordBox);
        FormLayoutHelper.AddRow(passwordLayout, 2, "Confirm New Password", _confirmPasswordBox);
        _passwordStatus = FormLayoutHelper.AddValidationLabel(passwordLayout, 3);

        var updatePasswordButton = new Button { Text = "Update Password", Width = 160, Height = 32 }.StyleAsSecondaryButton();
        updatePasswordButton.Click += async (_, _) => await ChangePasswordAsync();
        var updatePasswordRow = new Panel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(24, 4, 24, 0) };
        updatePasswordRow.Controls.Add(updatePasswordButton);

        // Added in reverse since each panel docks to Top.
        Controls.Add(updatePasswordRow);
        Controls.Add(passwordLayout);
        Controls.Add(passwordHeading);
        Controls.Add(dividerHost);
        Controls.Add(saveProfileRow);
        Controls.Add(profileLayout);
        Controls.Add(pictureRow);

        var closeButton = new Button { Text = "Close", Width = 100, Dock = DockStyle.Right }.StyleAsSecondaryButton();
        closeButton.Click += (_, _) => Close();
        var closeBar = new Panel { Dock = DockStyle.Bottom, Height = 56, Padding = new Padding(24, 0, 24, 16) };
        closeBar.Controls.Add(closeButton);
        Controls.Add(closeBar);
    }

    private void UploadPicture()
    {
        using var dialog = new OpenFileDialog { Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _ = SavePictureAsync(dialog.FileName);
    }

    private async Task SavePictureAsync(string sourceFilePath)
    {
        var user = _currentUserContext.CurrentUser!;
        var savedPath = ProfilePictureStore.SaveFromFile(user.UserId, sourceFilePath);

        await _userService.UpdateProfilePictureAsync(user.UserId, savedPath);
        user.ProfilePicturePath = savedPath;

        _avatar.SetUser(user.DisplayName, user.Username, ProfilePictureStore.Load(savedPath));
        ProfileUpdated?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveProfileAsync()
    {
        _profileError.Text = string.Empty;
        try
        {
            var user = _currentUserContext.CurrentUser!;
            await _userService.UpdateProfileAsync(user.UserId, _displayNameBox.Text.Trim(), _roleBox.Text.Trim(), _phoneBox.Text.Trim(), _emailBox.Text.Trim());

            user.DisplayName = _displayNameBox.Text.Trim();
            user.Role = _roleBox.Text.Trim();
            user.Phone = _phoneBox.Text.Trim();
            user.Email = _emailBox.Text.Trim();

            _avatar.SetUser(user.DisplayName, user.Username, ProfilePictureStore.Load(user.ProfilePicturePath));
            ProfileUpdated?.Invoke(this, EventArgs.Empty);

            _profileError.ForeColor = Theme.SuccessGreen;
            _profileError.Text = "Saved.";
        }
        catch (ValidationException ex)
        {
            _profileError.ForeColor = Theme.DangerRed;
            _profileError.Text = ex.Message;
        }
    }

    private async Task ChangePasswordAsync()
    {
        _passwordStatus.Text = string.Empty;
        try
        {
            if (_newPasswordBox.Text != _confirmPasswordBox.Text)
            {
                _passwordStatus.ForeColor = Theme.DangerRed;
                _passwordStatus.Text = "New passwords do not match.";
                return;
            }

            var user = _currentUserContext.CurrentUser!;
            await _userService.ChangePasswordAsync(user.UserId, _currentPasswordBox.Text, _newPasswordBox.Text);

            _currentPasswordBox.Clear();
            _newPasswordBox.Clear();
            _confirmPasswordBox.Clear();
            _passwordStatus.ForeColor = Theme.SuccessGreen;
            _passwordStatus.Text = "Password updated.";
        }
        catch (ValidationException ex)
        {
            _passwordStatus.ForeColor = Theme.DangerRed;
            _passwordStatus.Text = ex.Message;
        }
    }
}
