using System.Text.Json;
using KeyAuthDesktopPanel.Data;
using KeyAuthDesktopPanel.Models;
using KeyAuthDesktopPanel.Services;

namespace KeyAuthDesktopPanel;

public partial class Form1 : Form
{
    private readonly SqliteLicenseRepository _repository = new();
    private readonly KeyAuthBridgeClient _bridgeClient = new();
    private readonly KeyAuthPublicApiClient _publicApiClient = new();

    private TextBox _txtAppName = null!;
    private TextBox _txtBuyer = null!;
    private ComboBox _cmbDuration = null!;
    private TextBox _txtLatestKey = null!;

    private TextBox _txtApiUrl = null!;
    private TextBox _txtBridgeUrl = null!;
    private TextBox _txtOwnerId = null!;
    private TextBox _txtClientVersion = null!;
    private TextBox _txtSellerKey = null!;
    private TextBox _txtMask = null!;
    private TextBox _txtLevel = null!;
    private ComboBox _cmbCharMode = null!;

    private TextBox _txtValidateApp = null!;
    private TextBox _txtValidateKey = null!;

    private DataGridView _grid = null!;
    private Label _lblStatus = null!;
    private readonly string _settingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "KeyAuthDesktopPanel",
        "connection-settings.json"
    );

    public Form1()
    {
        InitializeComponent();
        BuildUi();
        LoadConnectionSettings();

        _repository.Initialize();
        RefreshGrid();
        SetStatus("Painel pronto.");
    }

    private void BuildUi()
    {
        Text = "KeyAuth Desktop Panel";
        Width = 1280;
        Height = 860;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 680);

        var main = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 380));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        Controls.Add(main);

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        top.Controls.Add(BuildGenerateGroup(), 0, 0);
        top.Controls.Add(BuildValidateGroup(), 1, 0);
        main.Controls.Add(top, 0, 0);

        var gridGroup = new GroupBox
        {
            Text = "Licencas locais salvas no app",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var gridLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        gridLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        gridGroup.Controls.Add(gridLayout);

        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };
        actionsPanel.Controls.Add(NewButton("Atualizar", (_, _) => RefreshGrid()));
        actionsPanel.Controls.Add(NewButton("Revogar Selecionada", (_, _) => RevokeSelected()));
        actionsPanel.Controls.Add(NewButton("Excluir Selecionada", (_, _) => DeleteSelected()));
        actionsPanel.Controls.Add(NewButton("Exportar JSON", (_, _) => ExportJson()));
        actionsPanel.Controls.Add(NewButton("Limpar Tudo", (_, _) => ClearAll()));

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false
        };

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Id",
            DataPropertyName = nameof(LicenseGridRow.Id),
            Visible = false
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Key",
            DataPropertyName = nameof(LicenseGridRow.Key),
            Width = 300
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "App",
            DataPropertyName = nameof(LicenseGridRow.AppName),
            Width = 130
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Cliente",
            DataPropertyName = nameof(LicenseGridRow.Buyer),
            Width = 140
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Expira",
            DataPropertyName = nameof(LicenseGridRow.ExpiresAt),
            Width = 120
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Status",
            DataPropertyName = nameof(LicenseGridRow.Status),
            Width = 110
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Ativacoes",
            DataPropertyName = nameof(LicenseGridRow.Activations),
            Width = 90
        });

        gridLayout.Controls.Add(actionsPanel, 0, 0);
        gridLayout.Controls.Add(_grid, 0, 1);
        main.Controls.Add(gridGroup, 0, 1);

        _lblStatus = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            ForeColor = Color.FromArgb(20, 20, 20)
        };
        main.Controls.Add(_lblStatus, 0, 2);
    }

    private GroupBox BuildGenerateGroup()
    {
        var group = new GroupBox
        {
            Text = "Gerar Keys (Local e KeyAuth)",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 18
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtAppName = new TextBox { Dock = DockStyle.Fill };
        _txtBuyer = new TextBox { Dock = DockStyle.Fill };
        _cmbDuration = new ComboBox
        {
            Dock = DockStyle.Left,
            DropDownStyle = ComboBoxStyle.DropDown,
            Width = 180
        };
        _cmbDuration.Items.AddRange(["7", "10", "20", "30", "90", "365", "lifetime"]);
        _cmbDuration.Text = "7";

        _txtLatestKey = new TextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Text = "Nenhuma key gerada."
        };

        _txtApiUrl = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = "https://keyauth.win/api/1.2/"
        };
        _txtBridgeUrl = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = "http://localhost/keyauth-source/api/desktop/"
        };
        _txtOwnerId = new TextBox { Dock = DockStyle.Fill };
        _txtClientVersion = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = "1.0"
        };
        _txtSellerKey = new TextBox { Dock = DockStyle.Fill, UseSystemPasswordChar = true };
        _txtMask = new TextBox
        {
            Dock = DockStyle.Fill,
            Text = "*****-*****-*****-*****-*****"
        };
        _txtLevel = new TextBox
        {
            Dock = DockStyle.Left,
            Width = 120,
            Text = "1"
        };
        _cmbCharMode = new ComboBox
        {
            Dock = DockStyle.Left,
            Width = 180,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        _cmbCharMode.Items.AddRange(["Misto (1)", "Maiusculo (2)", "Minusculo (3)"]);
        _cmbCharMode.SelectedIndex = 0;

        var btnGenerateLocal = NewButton("Gerar Local", (_, _) => GenerateLicenseLocal());
        var btnCopyLatest = NewButton("Copiar Ultima", (_, _) => CopyLatest());
        var btnGenerateCloud = NewButton("Gerar no KeyAuth", async (_, _) => await GenerateLicenseOnKeyAuthAsync());
        var btnSaveConfig = NewButton("Salvar Config", (_, _) => SaveConnectionSettings());
        var btnCopyClientConfig = NewButton("Copiar Config Cliente", (_, _) => CopyClientConfig());

        layout.Controls.Add(NewLabel("Nome do app"), 0, 0);
        layout.Controls.Add(_txtAppName, 1, 0);
        layout.Controls.Add(NewLabel("Cliente"), 0, 1);
        layout.Controls.Add(_txtBuyer, 1, 1);
        layout.Controls.Add(NewLabel("Duracao (dias)"), 0, 2);
        layout.Controls.Add(_cmbDuration, 1, 2);
        layout.Controls.Add(btnGenerateLocal, 1, 3);
        layout.Controls.Add(NewLabel("Ultima key"), 0, 4);
        layout.Controls.Add(_txtLatestKey, 1, 4);
        layout.Controls.Add(btnCopyLatest, 1, 5);

        var sep = new Label
        {
            Text = "Ligacao KeyAuth (site e exe usando mesma base)",
            Dock = DockStyle.Fill,
            Font = new Font(Font, FontStyle.Bold)
        };
        layout.Controls.Add(sep, 0, 6);
        layout.SetColumnSpan(sep, 2);

        layout.Controls.Add(NewLabel("API KeyAuth"), 0, 7);
        layout.Controls.Add(_txtApiUrl, 1, 7);
        layout.Controls.Add(NewLabel("Bridge URL"), 0, 8);
        layout.Controls.Add(_txtBridgeUrl, 1, 8);
        layout.Controls.Add(NewLabel("OwnerID"), 0, 9);
        layout.Controls.Add(_txtOwnerId, 1, 9);
        layout.Controls.Add(NewLabel("Versao cliente"), 0, 10);
        layout.Controls.Add(_txtClientVersion, 1, 10);
        layout.Controls.Add(NewLabel("SellerKey"), 0, 11);
        layout.Controls.Add(_txtSellerKey, 1, 11);
        layout.Controls.Add(NewLabel("Mascara"), 0, 12);
        layout.Controls.Add(_txtMask, 1, 12);
        layout.Controls.Add(NewLabel("Level"), 0, 13);
        layout.Controls.Add(_txtLevel, 1, 13);
        layout.Controls.Add(NewLabel("Caracteres"), 0, 14);
        layout.Controls.Add(_cmbCharMode, 1, 14);
        layout.Controls.Add(btnGenerateCloud, 1, 15);
        layout.Controls.Add(btnSaveConfig, 1, 16);
        layout.Controls.Add(btnCopyClientConfig, 1, 17);

        group.Controls.Add(layout);
        return group;
    }

    private GroupBox BuildValidateGroup()
    {
        var group = new GroupBox
        {
            Text = "Validar Key",
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _txtValidateApp = new TextBox { Dock = DockStyle.Fill };
        _txtValidateKey = new TextBox { Dock = DockStyle.Fill };

        var btnValidateLocal = NewButton("Validar Local", (_, _) => ValidateLicenseLocal());
        var btnValidateKeyAuth = NewButton("Validar no KeyAuth", async (_, _) => await ValidateLicenseOnKeyAuthAsync());

        layout.Controls.Add(NewLabel("Nome do app"), 0, 0);
        layout.Controls.Add(_txtValidateApp, 1, 0);
        layout.Controls.Add(NewLabel("Key"), 0, 1);
        layout.Controls.Add(_txtValidateKey, 1, 1);
        layout.Controls.Add(btnValidateLocal, 1, 2);
        layout.Controls.Add(btnValidateKeyAuth, 1, 3);

        var info = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "Validacao online usa API /1.2 (init + license)."
        };
        layout.Controls.Add(info, 1, 5);

        var info2 = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ForeColor = Color.DimGray,
            Text = "Se gerar no KeyAuth, a mesma key funciona no site e no exe."
        };
        layout.Controls.Add(info2, 1, 6);

        group.Controls.Add(layout);
        return group;
    }

    private static Label NewLabel(string text)
    {
        return new Label
        {
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill
        };
    }

    private static Button NewButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(10, 6, 10, 6),
            Margin = new Padding(0, 0, 8, 0)
        };
        button.Click += onClick;
        return button;
    }

    private void GenerateLicenseLocal()
    {
        var appName = _txtAppName.Text.Trim();
        var buyer = _txtBuyer.Text.Trim();

        if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(buyer))
        {
            SetStatus("Preencha app e cliente para gerar key local.");
            return;
        }

        if (!TryGetDurationDays(out var days))
        {
            SetStatus("Duracao invalida.");
            return;
        }

        var key = LicenseKeyGenerator.Create(appName);
        var now = DateTimeOffset.UtcNow;

        var license = new LicenseRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Key = key,
            AppName = appName,
            Buyer = buyer,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(days),
            Active = true,
            Activations = 0
        };

        _repository.Insert(license);
        MirrorGeneratedKey(appName, key);
        SetStatus("Key local gerada com sucesso.");
    }

    private async Task GenerateLicenseOnKeyAuthAsync()
    {
        try
        {
            var appName = _txtAppName.Text.Trim();
            var buyer = _txtBuyer.Text.Trim();
            var ownerId = _txtOwnerId.Text.Trim();
            var sellerKey = _txtSellerKey.Text.Trim();
            var bridgeUrl = _txtBridgeUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(sellerKey) || string.IsNullOrWhiteSpace(bridgeUrl))
            {
                SetStatus("Preencha app, ownerid, sellerkey e bridge URL.");
                return;
            }

            if (!TryGetDurationDays(out var days))
            {
                SetStatus("Duracao invalida.");
                return;
            }

            if (!int.TryParse(_txtLevel.Text.Trim(), out var level) || level < 1)
            {
                SetStatus("Level invalido.");
                return;
            }

            var characterMode = _cmbCharMode.SelectedIndex switch
            {
                1 => 2,
                2 => 3,
                _ => 1
            };

            var request = new KeyAuthBridgeGenerateRequest(
                BridgeUrl: bridgeUrl,
                OwnerId: ownerId,
                AppName: appName,
                SellerKey: sellerKey,
                Amount: 1,
                Mask: _txtMask.Text.Trim(),
                Duration: days,
                Expiry: 86400,
                Level: level,
                Note: string.IsNullOrWhiteSpace(buyer) ? "Gerado no painel desktop" : $"Cliente: {buyer}",
                CharacterMode: characterMode
            );

            var result = await _bridgeClient.GenerateAsync(request);
            if (!result.Success)
            {
                SetStatus($"Falha ao gerar no KeyAuth: {result.Message}");
                return;
            }

            if (result.Keys.Count == 0)
            {
                SetStatus("KeyAuth retornou sucesso sem keys.");
                return;
            }

            var key = result.Keys[0];
            SaveKeyLocallyFromCloud(appName, string.IsNullOrWhiteSpace(buyer) ? "cloud-user" : buyer, days, key);
            MirrorGeneratedKey(appName, key);
            SetStatus("Key gerada no KeyAuth e salva localmente.");
        }
        catch (Exception ex)
        {
            SetStatus($"Erro ao gerar no KeyAuth: {ex.Message}");
        }
    }

    private void SaveKeyLocallyFromCloud(string appName, string buyer, int days, string key)
    {
        var existing = _repository.FindByKey(key);
        if (existing is not null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var record = new LicenseRecord
        {
            Id = Guid.NewGuid().ToString("N"),
            Key = key,
            AppName = appName,
            Buyer = buyer,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(days),
            Active = true,
            Activations = 0
        };
        _repository.Insert(record);
        RefreshGrid();
    }

    private void MirrorGeneratedKey(string appName, string key)
    {
        _txtLatestKey.Text = key;
        _txtValidateKey.Text = key;
        _txtValidateApp.Text = appName;
        RefreshGrid();
    }

    private void CopyLatest()
    {
        var text = _txtLatestKey.Text.Trim();
        if (string.IsNullOrWhiteSpace(text) || text == "Nenhuma key gerada.")
        {
            SetStatus("Nao ha key para copiar.");
            return;
        }

        Clipboard.SetText(text);
        SetStatus("Ultima key copiada.");
    }

    private bool TryGetDurationDays(out int days)
    {
        var raw = _cmbDuration.Text.Trim();
        if (string.Equals(raw, "lifetime", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(raw, "life", StringComparison.OrdinalIgnoreCase))
        {
            days = 36500;
            return true;
        }

        return int.TryParse(raw, out days) && days > 0;
    }

    private void CopyClientConfig()
    {
        var apiUrl = _txtApiUrl.Text.Trim();
        var appName = _txtAppName.Text.Trim();
        var ownerId = _txtOwnerId.Text.Trim();
        var version = _txtClientVersion.Text.Trim();

        if (string.IsNullOrWhiteSpace(apiUrl) ||
            string.IsNullOrWhiteSpace(appName) ||
            string.IsNullOrWhiteSpace(ownerId) ||
            string.IsNullOrWhiteSpace(version))
        {
            SetStatus("Preencha API URL, app, ownerid e versao para copiar a config do cliente.");
            return;
        }

        var snippet =
            "// Configuracao do app cliente. Nao coloque SellerKey no cliente." + Environment.NewLine +
            $"private const string ApiUrl = \"{apiUrl}\";" + Environment.NewLine +
            $"private const string AppName = \"{appName}\";" + Environment.NewLine +
            $"private const string OwnerId = \"{ownerId}\";" + Environment.NewLine +
            $"private const string Version = \"{version}\";" + Environment.NewLine +
            Environment.NewLine +
            "var hwid = $\"{Environment.MachineName}-{Environment.UserName}\";" + Environment.NewLine +
            "var keyAuth = new KeyAuthPublicApiClient();" + Environment.NewLine +
            "var result = await keyAuth.ValidateLicenseAsync(ApiUrl, AppName, OwnerId, keyDigitada, hwid, Version);" + Environment.NewLine +
            "if (!result.Success) { MessageBox.Show(result.Message); return; }";

        Clipboard.SetText(snippet);
        SetStatus("Config do cliente copiada. SellerKey fica so no painel admin.");
    }

    private void ValidateLicenseLocal()
    {
        var appName = _txtValidateApp.Text.Trim();
        var key = _txtValidateKey.Text.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(key))
        {
            SetStatus("Informe app e key para validar.");
            return;
        }

        var license = _repository.FindByKey(key);
        if (license is null)
        {
            SetStatus("Key nao encontrada no banco local.");
            return;
        }

        if (!string.Equals(license.AppName, appName, StringComparison.OrdinalIgnoreCase))
        {
            SetStatus("Key encontrada localmente, mas app diferente.");
            return;
        }

        if (!license.Active)
        {
            SetStatus("Key local revogada.");
            return;
        }

        if (DateTimeOffset.UtcNow > license.ExpiresAtUtc)
        {
            SetStatus($"Key local expirada em {license.ExpiresAtUtc.LocalDateTime:dd/MM/yyyy}.");
            return;
        }

        var nextActivations = license.Activations + 1;
        _repository.RegisterActivation(license.Id, nextActivations);
        RefreshGrid();
        SetStatus($"Key local valida. Ativacoes: {nextActivations}.");
    }

    private async Task ValidateLicenseOnKeyAuthAsync()
    {
        try
        {
            var appName = _txtValidateApp.Text.Trim();
            var key = _txtValidateKey.Text.Trim().ToUpperInvariant();
            var ownerId = _txtOwnerId.Text.Trim();
            var apiUrl = _txtApiUrl.Text.Trim();

            if (string.IsNullOrWhiteSpace(appName) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(apiUrl))
            {
                SetStatus("Preencha app, key, ownerid e API URL.");
                return;
            }

            var hwid = $"{Environment.MachineName}-{Environment.UserName}";
            var result = await _publicApiClient.ValidateLicenseAsync(apiUrl, appName, ownerId, key, hwid);

            if (!result.Success)
            {
                SetStatus($"KeyAuth: {result.Message}");
                return;
            }

            SetStatus($"KeyAuth: {result.Message}");
        }
        catch (Exception ex)
        {
            SetStatus($"Erro na validacao online: {ex.Message}");
        }
    }

    private void RevokeSelected()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            SetStatus("Selecione uma key para revogar.");
            return;
        }

        _repository.Revoke(selected.Id);
        RefreshGrid();
        SetStatus("Key local revogada.");
    }

    private void DeleteSelected()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            SetStatus("Selecione uma key para excluir.");
            return;
        }

        var confirm = MessageBox.Show(
            "Deseja realmente excluir a key selecionada?",
            "Confirmar exclusao",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _repository.Delete(selected.Id);
        RefreshGrid();
        SetStatus("Key excluida.");
    }

    private void ClearAll()
    {
        var confirm = MessageBox.Show(
            "Deseja limpar todas as keys locais?",
            "Confirmar limpeza",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        _repository.ClearAll();
        RefreshGrid();
        SetStatus("Todas as keys locais foram removidas.");
    }

    private void ExportJson()
    {
        var items = _repository.GetAll();
        if (items.Count == 0)
        {
            SetStatus("Nao ha keys para exportar.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "JSON (*.json)|*.json",
            FileName = $"keyauth-keys-{DateTime.Now:yyyy-MM-dd}.json"
        };

        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        var json = JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(dialog.FileName, json);
        SetStatus($"Exportado: {dialog.FileName}");
    }

    private void RefreshGrid()
    {
        var rows = _repository
            .GetAll()
            .Select(item => new LicenseGridRow
            {
                Id = item.Id,
                Key = item.Key,
                AppName = item.AppName,
                Buyer = item.Buyer,
                ExpiresAt = item.ExpiresAtUtc.LocalDateTime.ToString("dd/MM/yyyy"),
                Status = GetStatus(item),
                Activations = item.Activations
            })
            .ToList();

        _grid.DataSource = rows;
    }

    private static string GetStatus(LicenseRecord item)
    {
        if (!item.Active)
        {
            return "revogada";
        }

        if (DateTimeOffset.UtcNow > item.ExpiresAtUtc)
        {
            return "expirada";
        }

        return "ativa";
    }

    private LicenseGridRow? GetSelected()
    {
        if (_grid.SelectedRows.Count == 0)
        {
            return null;
        }

        return _grid.SelectedRows[0].DataBoundItem as LicenseGridRow;
    }

    private void SetStatus(string text)
    {
        _lblStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {text}";
    }

    private void SaveConnectionSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var settings = new ConnectionSettings
            {
                AppName = _txtAppName.Text.Trim(),
                ApiUrl = _txtApiUrl.Text.Trim(),
                BridgeUrl = _txtBridgeUrl.Text.Trim(),
                OwnerId = _txtOwnerId.Text.Trim(),
                ClientVersion = _txtClientVersion.Text.Trim(),
                SellerKey = _txtSellerKey.Text.Trim(),
                Mask = _txtMask.Text.Trim(),
                Level = _txtLevel.Text.Trim()
            };

            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsFilePath, json);
            SetStatus("Configuracao salva.");
        }
        catch (Exception ex)
        {
            SetStatus($"Falha ao salvar configuracao: {ex.Message}");
        }
    }

    private void LoadConnectionSettings()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return;
            }

            var json = File.ReadAllText(_settingsFilePath);
            var settings = JsonSerializer.Deserialize<ConnectionSettings>(json);
            if (settings is null)
            {
                return;
            }

            _txtAppName.Text = string.IsNullOrWhiteSpace(settings.AppName) ? _txtAppName.Text : settings.AppName;
            _txtApiUrl.Text = string.IsNullOrWhiteSpace(settings.ApiUrl) ? _txtApiUrl.Text : settings.ApiUrl;
            _txtBridgeUrl.Text = string.IsNullOrWhiteSpace(settings.BridgeUrl) ? _txtBridgeUrl.Text : settings.BridgeUrl;
            _txtOwnerId.Text = string.IsNullOrWhiteSpace(settings.OwnerId) ? _txtOwnerId.Text : settings.OwnerId;
            _txtClientVersion.Text = string.IsNullOrWhiteSpace(settings.ClientVersion) ? _txtClientVersion.Text : settings.ClientVersion;
            _txtSellerKey.Text = string.IsNullOrWhiteSpace(settings.SellerKey) ? _txtSellerKey.Text : settings.SellerKey;
            _txtMask.Text = string.IsNullOrWhiteSpace(settings.Mask) ? _txtMask.Text : settings.Mask;
            _txtLevel.Text = string.IsNullOrWhiteSpace(settings.Level) ? _txtLevel.Text : settings.Level;
            if (string.IsNullOrWhiteSpace(_txtValidateApp.Text))
            {
                _txtValidateApp.Text = _txtAppName.Text;
            }
        }
        catch
        {
            // Ignore settings read errors and keep defaults.
        }
    }

    private sealed class LicenseGridRow
    {
        public string Id { get; init; } = string.Empty;
        public string Key { get; init; } = string.Empty;
        public string AppName { get; init; } = string.Empty;
        public string Buyer { get; init; } = string.Empty;
        public string ExpiresAt { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public int Activations { get; init; }
    }

    private sealed class ConnectionSettings
    {
        public string AppName { get; init; } = string.Empty;
        public string ApiUrl { get; init; } = string.Empty;
        public string BridgeUrl { get; init; } = string.Empty;
        public string OwnerId { get; init; } = string.Empty;
        public string ClientVersion { get; init; } = string.Empty;
        public string SellerKey { get; init; } = string.Empty;
        public string Mask { get; init; } = string.Empty;
        public string Level { get; init; } = string.Empty;
    }
}
