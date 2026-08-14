using System.Drawing.Drawing2D;
using DartERP.Application;
using DartERP.Application.Services;
using DartERP.Application.Validation;
using DartERP.WinForms.Styling;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DartERP.WinForms.Forms;

public class LoginForm : Form
{
    private const int ContentWidth = 460;
    private static readonly string[] RoleSuggestions =
    [
        "Plant Administrator", "Production Supervisor", "Compliance Officer",
        "Machine Operator", "Quality Inspector", "Warehouse Lead",
    ];

    private readonly UserService _userService;
    private readonly CurrentUserContext _currentUserContext;

    private readonly WebView2 _webView = new() { Dock = DockStyle.Fill };
    private readonly Button _signInTab;
    private readonly Button _signUpTab;
    private readonly Panel _signInPanel;
    private readonly Panel _signUpPanel;

    private readonly TextBox _signInUsername = new TextBox().StyleAsInput();
    private readonly TextBox _signInPassword = new TextBox().StyleAsInput();
    private readonly Label _signInError;

    private readonly TextBox _signUpUsername = new TextBox().StyleAsInput();
    private readonly TextBox _signUpEmail = new TextBox().StyleAsInput();
    private readonly TextBox _signUpDisplayName = new TextBox().StyleAsInput();
    private readonly ComboBox _signUpRole;
    private readonly TextBox _signUpPhone = new TextBox().StyleAsInput();
    private readonly TextBox _signUpPassword = new TextBox().StyleAsInput();
    private readonly TextBox _signUpConfirmPassword = new TextBox().StyleAsInput();
    private readonly Label _signUpError;

    public LoginForm(UserService userService, CurrentUserContext currentUserContext)
    {
        _userService = userService;
        _currentUserContext = currentUserContext;

        Text = "Sign in to DartERP";
        Font = Theme.FontBody;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        StartPosition = FormStartPosition.CenterScreen;
        MaximizeBox = false;
        MinimizeBox = true;
        ClientSize = new Size(1040, 640);
        BackColor = Theme.CardBackground;
        Icon = AppAssets.WindowIcon;

        _signInPassword.UseSystemPasswordChar = true;
        _signUpPassword.UseSystemPasswordChar = true;
        _signUpConfirmPassword.UseSystemPasswordChar = true;

        _signUpRole = BuildRoleComboBox();

        var videoPanel = BuildVideoPanel();

        var (toggleHost, signInTab, signUpTab) = BuildSegmentedToggle();
        _signInTab = signInTab;
        _signUpTab = signUpTab;

        _signInPanel = BuildSignInPanel(out _signInError);
        _signUpPanel = BuildSignUpPanel(out _signUpError);

        _signInTab.Click += (_, _) => ShowSignIn();
        _signUpTab.Click += (_, _) => ShowSignUp();

        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Top,
        };
        stack.Controls.Add(BuildLogo());
        stack.Controls.Add(BuildHeadline());
        stack.Controls.Add(BuildSubtext());
        stack.Controls.Add(Spacer(16));
        stack.Controls.Add(toggleHost);
        stack.Controls.Add(Spacer(20));
        stack.Controls.Add(_signInPanel);
        stack.Controls.Add(_signUpPanel);

        var formPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.CardBackground,
            Padding = new Padding(60, 44, 60, 28),
            // Sign In fits comfortably in the fixed 640px-tall window, but
            // Sign Up's extra fields (email, role, phone, confirm password)
            // push the total content past that — without this, "Confirm
            // Password" and the submit button just get clipped off the
            // bottom with no way to reach them. AutoScroll only kicks in
            // when content actually overflows, so Sign In stays exactly as
            // it looked before.
            AutoScroll = true,
        };
        formPanel.Controls.Add(stack);

        Controls.Add(formPanel);
        Controls.Add(videoPanel);

        ShowSignIn();
        Load += async (_, _) => await InitializeVideoAsync();
    }

    private static Panel Spacer(int height) => new() { Height = height, Width = ContentWidth };

    private static PictureBox BuildLogo() => new()
    {
        Image = AppAssets.Logo,
        SizeMode = PictureBoxSizeMode.Zoom,
        Width = 150,
        Height = 42,
        Margin = new Padding(0, 0, 0, 20),
    };

    private static Label BuildHeadline() => new()
    {
        Text = "Welcome to DERP",
        Font = Theme.FontHeader,
        ForeColor = Theme.TextPrimary,
        AutoSize = false,
        Width = ContentWidth,
        Height = 32,
    };

    private static Label BuildSubtext() => new()
    {
        Text = "Sign in to manage production, inventory, and compliance.",
        Font = Theme.FontBody,
        ForeColor = Theme.TextSecondary,
        AutoSize = false,
        Width = ContentWidth,
        Height = 22,
    };

    private (Panel host, Button signIn, Button signUp) BuildSegmentedToggle()
    {
        var host = new Panel { Width = ContentWidth, Height = 40, BackColor = Theme.AppBackground };
        host.ApplyRoundedRegion(20);

        var signIn = new Button
        {
            Text = "Sign In",
            Dock = DockStyle.Left,
            Width = ContentWidth / 2,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.FontBodyBold,
            Cursor = Cursors.Hand,
        };
        signIn.FlatAppearance.BorderSize = 0;

        var signUp = new Button
        {
            Text = "Sign Up",
            Dock = DockStyle.Right,
            Width = ContentWidth / 2,
            FlatStyle = FlatStyle.Flat,
            Font = Theme.FontBodyBold,
            Cursor = Cursors.Hand,
        };
        signUp.FlatAppearance.BorderSize = 0;

        host.Controls.Add(signUp);
        host.Controls.Add(signIn);
        return (host, signIn, signUp);
    }

    private void ShowSignIn()
    {
        _signInPanel.Visible = true;
        _signUpPanel.Visible = false;
        StyleToggle(active: _signInTab, inactive: _signUpTab);
    }

    private void ShowSignUp()
    {
        _signInPanel.Visible = false;
        _signUpPanel.Visible = true;
        StyleToggle(active: _signUpTab, inactive: _signInTab);
    }

    private static void StyleToggle(Button active, Button inactive)
    {
        active.BackColor = Theme.SidebarBackground;
        active.ForeColor = Color.White;
        active.ApplyRoundedRegion(18);

        inactive.BackColor = Theme.AppBackground;
        inactive.ForeColor = Theme.TextSecondary;
        inactive.Region = null;
    }

    private Panel BuildSignInPanel(out Label errorLabel)
    {
        var panel = new Panel { Width = ContentWidth, AutoSize = true };
        var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Width = ContentWidth };

        stack.Controls.Add(BuildField("Username", _signInUsername));
        stack.Controls.Add(BuildField("Password", _signInPassword));

        errorLabel = BuildErrorLabel();
        stack.Controls.Add(errorLabel);

        var signInButton = new Button { Text = "Sign In", Width = ContentWidth, Height = 38, Margin = new Padding(0, 4, 0, 8) }.StyleAsPrimaryButton();
        signInButton.Click += async (_, _) => await SignInAsync();
        stack.Controls.Add(signInButton);

        var hint = new Label
        {
            Text = "Demo login: admin / Password123!",
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            AutoSize = false,
            Width = ContentWidth,
            Height = 18,
        };
        stack.Controls.Add(hint);

        panel.Controls.Add(stack);
        return panel;
    }

    private Panel BuildSignUpPanel(out Label errorLabel)
    {
        var panel = new Panel { Width = ContentWidth, AutoSize = true };
        var stack = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Width = ContentWidth };

        stack.Controls.Add(BuildField("Username", _signUpUsername));
        stack.Controls.Add(BuildField("Email", _signUpEmail));
        stack.Controls.Add(BuildField("Display Name", _signUpDisplayName));
        stack.Controls.Add(BuildField("Role", _signUpRole));
        stack.Controls.Add(BuildField("Phone (optional)", _signUpPhone));
        stack.Controls.Add(BuildField("Password", _signUpPassword));
        stack.Controls.Add(BuildField("Confirm Password", _signUpConfirmPassword));

        errorLabel = BuildErrorLabel();
        stack.Controls.Add(errorLabel);

        var createButton = new Button { Text = "Create Account", Width = ContentWidth, Height = 38, Margin = new Padding(0, 4, 0, 8) }.StyleAsPrimaryButton();
        createButton.Click += async (_, _) => await SignUpAsync();
        stack.Controls.Add(createButton);

        panel.Controls.Add(stack);
        return panel;
    }

    private static ComboBox BuildRoleComboBox()
    {
        // Not using StyleAsInput() here on purpose — it locks the ComboBox
        // to DropDownStyle.DropDownList, but Role should be a suggestion,
        // not a hard-coded set of choices (a role field with a rigid enum
        // would mean adding a code change every time someone's title
        // doesn't match one of a handful of options).
        var comboBox = new ComboBox
        {
            Font = Theme.FontBody,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDown,
            BackColor = Theme.CardBackground,
            ForeColor = Theme.TextPrimary,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems,
        };
        comboBox.Items.AddRange(RoleSuggestions);
        // NOTE: without this, the box shows the first item ("Plant
        // Administrator") pre-filled and highlighted the moment it's built
        // — never anything I set explicitly. Editable ComboBoxes with
        // AutoCompleteMode.SuggestAppend do this: the very first paint
        // treats the empty Text as "matches everything" and appends the
        // first list entry as a suggestion, auto-selected. Forcing it back
        // to blank right after populating Items is what actually sticks.
        comboBox.SelectedIndex = -1;
        comboBox.Text = string.Empty;
        return comboBox;
    }

    private static Panel BuildField(string labelText, Control input)
    {
        var panel = new Panel { Width = ContentWidth, Height = 60, Margin = new Padding(0, 0, 0, 4) };
        var label = new Label
        {
            Text = labelText,
            Font = Theme.FontSmall,
            ForeColor = Theme.TextSecondary,
            Dock = DockStyle.Top,
            Height = 18,
        };
        input.Dock = DockStyle.Top;
        input.Height = 30;

        panel.Controls.Add(input);
        panel.Controls.Add(label);
        return panel;
    }

    private static Label BuildErrorLabel() => new()
    {
        Text = string.Empty,
        Font = Theme.FontSmall,
        ForeColor = Theme.DangerRed,
        AutoSize = false,
        Width = ContentWidth,
        Height = 20,
        Margin = new Padding(0, 0, 0, 4),
    };

    private async Task SignInAsync()
    {
        _signInError.Text = string.Empty;
        try
        {
            var user = await _userService.AuthenticateAsync(_signInUsername.Text.Trim(), _signInPassword.Text);
            if (user is null)
            {
                _signInError.Text = "Invalid username or password.";
                return;
            }

            _currentUserContext.SignIn(user);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception)
        {
            _signInError.Text = "Unable to sign in. Please try again.";
        }
    }

    private async Task SignUpAsync()
    {
        _signUpError.Text = string.Empty;
        try
        {
            if (_signUpPassword.Text != _signUpConfirmPassword.Text)
            {
                _signUpError.Text = "Passwords do not match.";
                return;
            }

            var user = await _userService.RegisterAsync(
                _signUpUsername.Text.Trim(),
                _signUpEmail.Text.Trim(),
                _signUpPassword.Text,
                _signUpDisplayName.Text.Trim(),
                _signUpRole.Text.Trim(),
                _signUpPhone.Text.Trim());

            // Skip the round trip back to Sign In — they just proved they
            // know the password by typing it twice, no reason to make them
            // type it a third time.
            _currentUserContext.SignIn(user);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ValidationException ex)
        {
            _signUpError.Text = ex.Message;
        }
        catch (Exception)
        {
            _signUpError.Text = "Unable to create the account. Please try again.";
        }
    }

    private Panel BuildVideoPanel()
    {
        var panel = new Panel { Dock = DockStyle.Left, Width = 460, BackColor = Color.Black };
        panel.Controls.Add(_webView);
        return panel;
    }

    private async Task InitializeVideoAsync()
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DartERP", "WebView2");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await _webView.EnsureCoreWebView2Async(environment);

            _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;

            var htmlPath = Path.Combine(AppContext.BaseDirectory, "Assets", "login.html");
            _webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);
        }
        catch
        {
            // WebView2 Runtime genuinely missing (rare — it ships with
            // Windows 11 and rides along with Edge updates on Windows 10)
            // or the video failed to init for some other reason. Either
            // way, a login screen that crashes over a background video is
            // a much worse outcome than just not showing one.
            _webView.Visible = false;
            _webView.Parent?.Controls.Add(BuildFallbackPanel());
        }
    }

    private static Panel BuildFallbackPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill };
        panel.Paint += (_, e) =>
        {
            using var brush = new LinearGradientBrush(
                panel.ClientRectangle, Theme.SidebarBackground, Theme.AccentPrimary, LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillRectangle(brush, panel.ClientRectangle);
        };

        var logo = new PictureBox
        {
            Image = AppAssets.Logo,
            SizeMode = PictureBoxSizeMode.Zoom,
            Width = 220,
            Height = 100,
            Anchor = AnchorStyles.None,
        };
        panel.Controls.Add(logo);
        panel.Layout += (_, _) => logo.Location = new Point((panel.Width - logo.Width) / 2, (panel.Height - logo.Height) / 2);

        return panel;
    }
}
