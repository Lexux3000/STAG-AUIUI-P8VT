﻿using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Pulumi;
using Pulumi.AzureNative.Resources;
using Pulumi.AzureNative.KeyVault;
using Pulumi.AzureNative.Sql;
using Pulumi.AzureNative.Sql.Inputs;
using Pulumi.AzureNative.Web;
using Pulumi.AzureNative.Web.Inputs;
using Pulumi.Random;
using AzureNative = Pulumi.AzureNative;

// ReSharper disable InconsistentNaming

return await Pulumi.Deployment.RunAsync(() =>
{
    // Create passwords
    var adminDbUsername = "cngroup-utb-administrator";
    var adminDbPassword = CreatePassword("cngroup-utb--2023-administrator-password");

    var internalTestDbUsername = "cngroup_utb_administrator_internaltest";
    var internalTestDbPassword = CreatePassword("cngroup-utb--2023-InternalTest-password");

    var toPujdeDbUsername = "cngroup_utb_administrator_topujde";
    var toPujdeDbPassword = CreatePassword("cngroup-utb--2023-ToPujde-password");

    var amundsenDbUsername = "cngroup_utb_administrator_admundsen";
    var amundsenDbPassword = CreatePassword("cngroup-utb--2023-Amundsen-password");

    var hdKvalitaDbUsername = "cngroup_utb_administrator_hdkvalita";
    var hdKvalitaDbPassword = CreatePassword("cngroup-utb--2023-HD-kvalita-password");

    var nevimeDbUsername = "cngroup_utb_administrator_nevime";
    var nevimeDbPassword = CreatePassword("cngroup-utb--2023-Nevime-password");

    var orzDbUsername = "cngroup_utb_administrator_orz";
    var orzDbPassword = CreatePassword("cngroup-utb--2023-ORZ-password");

    var sempaDbUsername = "cngroup_utb_administrator_sempa";
    var sempaDbPassword = CreatePassword("cngroup-utb--2023-Sempa-password");

    Output<string> CreatePassword(string name)
    {
        return new RandomPassword(name,
                new RandomPasswordArgs { Length = 18, Special = true, OverrideSpecial = "%!?", MinSpecial = 3 })
            .Result;
    }


    // Create an Azure Resource Group
    var resourceGroup = new ResourceGroup("cngroup-utb--2023", new ResourceGroupArgs
    {
        ResourceGroupName = "cngroup-utb--2023",
        Location = "westeurope"
    });

    // Create azure key vault
    var keyVault = new Vault("cngroup0utb02023", new()
    {
        VaultName = "cngroup0utb02023",
        Location = "westeurope",
        Properties = new AzureNative.KeyVault.Inputs.VaultPropertiesArgs
        {
            AccessPolicies = new[]
            {
                new AzureNative.KeyVault.Inputs.AccessPolicyEntryArgs
                {
                    ObjectId = "03ab3783-6c0a-4010-a32e-d24a21e31481",
                    Permissions = new AzureNative.KeyVault.Inputs.PermissionsArgs
                    {
                        Secrets = new InputList<Union<string, SecretPermissions>>
                        {
                            "get",
                            "list",
                            "set",
                            "delete",
                            "backup",
                            "restore",
                            "recover",
                            "purge",
                        },
                    },
                    TenantId = "0f823349-2c12-431b-a03c-b2c0a43d6fb4",
                },
            },

            EnabledForDeployment = true,
            EnabledForDiskEncryption = true,
            EnabledForTemplateDeployment = true,
            Sku = new AzureNative.KeyVault.Inputs.SkuArgs
            {
                Family = "A",
                Name = SkuName.Standard,
            },
            TenantId = "0f823349-2c12-431b-a03c-b2c0a43d6fb4"
        },
        ResourceGroupName = resourceGroup.Name
    });
    CreateSecret("cngroup-utb--2023-sql-admin-password", adminDbPassword);
    CreateSecret("cngroup-utb--2023-sql-InternalTest-password", internalTestDbPassword);
    CreateSecret("cngroup-utb--2023-sql-ToPujde-password", toPujdeDbPassword);
    CreateSecret("cngroup-utb--2023-sql-Admundsen-password", amundsenDbPassword);
    CreateSecret("cngroup-utb--2023-sql-HD-Kvalita-password", hdKvalitaDbPassword);
    CreateSecret("cngroup-utb--2023-sql-Nevime-password", nevimeDbPassword);
    CreateSecret("cngroup-utb--2023-sql-ORZ-password", orzDbPassword);
    CreateSecret("cngroup-utb--2023-sql-Sempa-password", sempaDbPassword);

    Secret CreateSecret(string name, Output<string> value)
    {
        return new Secret(name, new SecretArgs()
        {
            ResourceGroupName = resourceGroup.Name,
            SecretName = name,
            VaultName = keyVault.Name,
            Properties = new AzureNative.KeyVault.Inputs.SecretPropertiesArgs
            {
                Value = value,
            },
        });
    }

    // Create azure sql server
    var sqlServer = new Server("cngroup-utb--2023", new()
    {
        ServerName = "cngroup-utb--2023",
        ResourceGroupName = resourceGroup.Name,
        Location = resourceGroup.Location,
        Version = "12.0",
        AdministratorLogin = adminDbUsername,
        AdministratorLoginPassword = adminDbPassword
    });

    CreateDatabase("cngroup-utb--2023-OS-InternalTest", sqlServer, resourceGroup);
    CreateDatabase("cngroup-utb--2023-OS-ToPujde", sqlServer, resourceGroup);
    CreateDatabase("cngroup-utb--2023-OS-Amundsen", sqlServer, resourceGroup);
    CreateDatabase("cngroup-utb--2023-OS-HD-kvalita", sqlServer, resourceGroup);
    CreateDatabase("cngroup-utb--2023-OS-Nevime", sqlServer, resourceGroup);
    CreateDatabase("cngroup-utb--2023-OS-ORZ", sqlServer, resourceGroup);
    CreateDatabase("cngroup-utb--2023-OS-Sempa", sqlServer, resourceGroup);

    Database CreateDatabase(string name, Server server, ResourceGroup rg)
    {
        return new Database(name, new()
        {
            DatabaseName = name,
            ResourceGroupName = rg.Name,
            Location = rg.Location,
            ServerName = server.Name,
            Sku = new SkuArgs
            {
                Name = "Basic",
                Tier = "Basic",
                Size = "2"
            },
            RequestedBackupStorageRedundancy = RequestedBackupStorageRedundancy.Local
        });
    }

    // create db users
    Output.Tuple(adminDbPassword, internalTestDbPassword, sqlServer.Name)
        .Apply(async output => await CreateUser(
            output.Item3, "cngroup-utb--2023-OS-InternalTest",
            internalTestDbUsername, output.Item1, output.Item2))
        .Apply(x =>
        {
            x.Wait();
            return x.Id;
        });

    Output.Tuple(adminDbPassword, toPujdeDbPassword, sqlServer.Name)
        .Apply(async output => await CreateUser(
            output.Item3, "cngroup-utb--2023-OS-ToPujde", toPujdeDbUsername,
            output.Item1, output.Item2))
        .Apply(x =>
        {
            x.Wait();
            return x.Id;
        });

    Output.Tuple(adminDbPassword, amundsenDbPassword, sqlServer.Name)
        .Apply(async output => await CreateUser(
            output.Item3, "cngroup-utb--2023-OS-Amundsen", amundsenDbUsername,
            output.Item1, output.Item2))
        .Apply(x =>
        {
            x.Wait();
            return x.Id;
        });

    Output.Tuple(adminDbPassword, hdKvalitaDbPassword, sqlServer.Name)
        .Apply(async output => await CreateUser(
            output.Item3, "cngroup-utb--2023-OS-HD-kvalita", hdKvalitaDbUsername,
            output.Item1, output.Item2))
        .Apply(x =>
        {
            x.Wait();
            return x.Id;
        });

    Output.Tuple(adminDbPassword, nevimeDbPassword, sqlServer.Name)
        .Apply(async output => await CreateUser(
            output.Item3, "cngroup-utb--2023-OS-Nevime", nevimeDbUsername,
            output.Item1, output.Item2))
        .Apply(x =>
        {
            x.Wait();
            return x.Id;
        });

    Output.Tuple(adminDbPassword, orzDbPassword, sqlServer.Name)
        .Apply(async output => await CreateUser(
            output.Item3, "cngroup-utb--2023-OS-ORZ", orzDbUsername,
            output.Item1, output.Item2))
        .Apply(x =>
        {
            x.Wait();
            return x.Id;
        });

    Output.Tuple(adminDbPassword, sempaDbPassword, sqlServer.Name)
        .Apply(async output => await CreateUser(
            output.Item3, "cngroup-utb--2023-OS-Sempa", sempaDbUsername,
            output.Item1, output.Item2))
        .Apply(x =>
        {
            x.Wait();
            return x.Id;
        });

    async Task CreateUser(string sqlServerName, string databaseName, string username, string adminPassword,
        string password)
    {
        var connString = GetConnectionString(sqlServerName, databaseName, adminPassword);
        await using var sqlConn = new SqlConnection(connString);
        await sqlConn.OpenAsync();

        StringBuilder sb = new();
        sb.AppendLine($"CREATE USER {username} WITH PASSWORD='{password}'");
        sb.AppendLine($"EXEC sp_addrolemember N'db_datareader', N'{username}'");
        sb.AppendLine($"EXEC sp_addrolemember N'db_datawriter', N'{username}'");
        sb.AppendLine($"EXEC sp_addrolemember N'db_ddladmin', N'{username}'");
        await using SqlCommand dropCmd = new($"DROP USER IF EXISTS {username}", sqlConn);
        await using SqlCommand createCmd = new(sb.ToString(), sqlConn);
        await dropCmd.ExecuteNonQueryAsync();
        await createCmd.ExecuteNonQueryAsync();
    }

    string GetConnectionString(string sqlServerName, string databaseName, string adminPassword)
    {
        return $"Server=tcp:{sqlServerName}.database.windows.net,1433;" +
               $"Initial Catalog={databaseName};" +
               $"Persist Security Info=False;" +
               $"User ID={adminDbUsername};" +
               $"Password={adminPassword};" +
               $"MultipleActiveResultSets=False;" +
               $"Encrypt=True;" +
               $"TrustServerCertificate=False;" +
               $"Connection Timeout=30;";
    }

    // Apps
    var appServicePlan = new AppServicePlan("cngroup-utb--2023", new()
    {
        Kind = "app",
        Location = "westeurope",
        Name = "cngroup-utb--2023",
        ResourceGroupName = resourceGroup.Name,
        Sku = new SkuDescriptionArgs
        {
            Capacity = 1,
            Family = "B",
            Name = "B1",
            Size = "B1",
            Tier = "Basic",
        }
    });
    CreateWebApp("cngroup-utb--2023-OS-InternalTest", appServicePlan);
    CreateWebApp("cngroup-utb--2023-OS-ToPujde", appServicePlan);
    CreateWebApp("cngroup-utb--2023-OS-Amundsen", appServicePlan);
    CreateWebApp("cngroup-utb--2023-OS-HD-kvalita", appServicePlan);
    CreateWebApp("cngroup-utb--2023-OS-Nevime", appServicePlan);
    CreateWebApp("cngroup-utb--2023-OS-ORZ", appServicePlan);
    CreateWebApp("cngroup-utb--2023-OS-Sempa", appServicePlan);

    WebApp CreateWebApp(string name, AppServicePlan appServicePlan)
    {
        return new WebApp(name, new WebAppArgs
        {
            Name = name,
            ResourceGroupName = resourceGroup.Name,
            RedundancyMode = RedundancyMode.None,
            Kind = "app",
            Location = "westeurope",
            ServerFarmId = appServicePlan.Name
        });
    }
});


// interpolatedStringHandler.AppendLiteral("CREATE USER ");
// interpolatedStringHandler.AppendFormatted(username);
// interpolatedStringHandler.AppendLiteral(" WITH PASSWORD='");
// interpolatedStringHandler.AppendFormatted(password); 
// interpolatedStringHandler.AppendLiteral("'");
// ref StringBuilder.AppendInterpolatedStringHandler local1 = ref interpolatedStringHandler;
// stringBuilder3.AppendLine(ref local1);
// StringBuilder stringBuilder4 = stringBuilder1;
// StringBuilder stringBuilder5 = stringBuilder4;
// interpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(43, 1, stringBuilder4);
// interpolatedStringHandler.AppendLiteral("EXEC sp_addrolemember N'db_datareader', N'");
// interpolatedStringHandler.AppendFormatted(username);
// interpolatedStringHandler.AppendLiteral("'");
// ref StringBuilder.AppendInterpolatedStringHandler local2 = ref interpolatedStringHandler;
// stringBuilder5.AppendLine(ref local2);
// StringBuilder stringBuilder6 = stringBuilder1;
// StringBuilder stringBuilder7 = stringBuilder6;
// interpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(43, 1, stringBuilder6);
// interpolatedStringHandler.AppendLiteral("EXEC sp_addrolemember N'db_datawriter', N'");
// interpolatedStringHandler.AppendFormatted(username);
// interpolatedStringHandler.AppendLiteral("'");
// ref StringBuilder.AppendInterpolatedStringHandler local3 = ref interpolatedStringHandler;
// stringBuilder7.AppendLine(ref local3);
// StringBuilder stringBuilder8 = stringBuilder1;
// StringBuilder stringBuilder9 = stringBuilder8;
// interpolatedStringHandler = new StringBuilder.AppendInterpolatedStringHandler(41, 1, stringBuilder8);
// interpolatedStringHandler.AppendLiteral("EXEC sp_addrolemember N'db_ddladmin', N'");