﻿using DGP.Genshin.Common.Core.DependencyInjection;
using DGP.Genshin.Controls.Launching;
using DGP.Genshin.Controls.TitleBarButtons;
using DGP.Genshin.DataModels.Launching;
using DGP.Genshin.Helpers;
using DGP.Genshin.Services.Abstratcions;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using ModernWpf.Controls;
using ModernWpf.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DGP.Genshin.ViewModels.TitleBarButtons
{
    [ViewModel(ViewModelType.Transient)]
    public class LaunchViewModel : ObservableObject
    {
        private readonly ILaunchService launchService;
        private readonly ISettingService settingService;

        private TitleBarButton? View;

        #region Observables
        private List<LaunchScheme> knownSchemes = new()
        {
            new LaunchScheme(name: "官服 | 天空岛", channel: "1", cps: "pcadbdpz", subChannel: "1"),
            new LaunchScheme(name: "B 服 | 世界树", channel: "14", cps: "bilibili", subChannel: "0")
        };
        private LaunchScheme? currentScheme;
        private bool isBorderless;
        private bool isFullScreen;
        private ObservableCollection<GenshinAccount> accounts = new();
        private GenshinAccount? selectedAccount;
        private IAsyncRelayCommand<TitleBarButton> openUICommand;
        private IAsyncRelayCommand<string> launchCommand;
        private IRelayCommand closeUICommand;
        private IAsyncRelayCommand deleteAccountCommand;

        /// <summary>
        /// 已知的启动方案
        /// </summary>
        public List<LaunchScheme> KnownSchemes
        {
            get => knownSchemes;
            set => SetProperty(ref knownSchemes, value);
        }
        /// <summary>
        /// 当前启动方案
        /// </summary>
        public LaunchScheme? CurrentScheme
        {
            get => currentScheme;
            set
            {
                SetProperty(ref currentScheme, value);
                launchService.SaveLaunchScheme(value);
            }
        }
        /// <summary>
        /// 是否全屏
        /// </summary>
        public bool IsFullScreen
        {
            get => isFullScreen;
            set
            {
                SetProperty(ref isFullScreen, value);
                settingService[Setting.IsFullScreen] = value;
            }
        }
        /// <summary>
        /// 是否无边框窗口
        /// </summary>
        public bool IsBorderless
        {
            get => isBorderless;
            set
            {
                SetProperty(ref isBorderless, value);
                settingService[Setting.IsBorderless] = value;
            }
        }
        public ObservableCollection<GenshinAccount> Accounts
        {
            get => accounts;
            set => SetProperty(ref accounts, value);
        }
        public GenshinAccount? SelectedAccount
        {
            get => selectedAccount;
            set
            {
                SetProperty(ref selectedAccount, value);
                launchService.SetToRegistry(value);
            }
        }
        public IAsyncRelayCommand<TitleBarButton> OpenUICommand
        {
            get => openUICommand;
            [MemberNotNull(nameof(openUICommand))]
            set => SetProperty(ref openUICommand, value);
        }
        public IRelayCommand CloseUICommand
        {
            get => closeUICommand;
            [MemberNotNull(nameof(closeUICommand))]
            set => SetProperty(ref closeUICommand, value);
        }
        public IAsyncRelayCommand<string> LaunchCommand
        {
            get => launchCommand;
            [MemberNotNull(nameof(launchCommand))]
            set => SetProperty(ref launchCommand, value);
        }
        public IAsyncRelayCommand DeleteAccountCommand
        {
            get => deleteAccountCommand;
            [MemberNotNull(nameof(deleteAccountCommand))]
            set => SetProperty(ref deleteAccountCommand, value);
        }
        #endregion

        public LaunchViewModel(ILaunchService launchService, ISettingService settingService)
        {
            this.launchService = launchService;
            this.settingService = settingService;

            Accounts = launchService.LoadAllAccount();
            SelectedAccount = Accounts.FirstOrDefault();
            IsBorderless = settingService.GetOrDefault(Setting.IsBorderless, false);
            IsFullScreen = settingService.GetOrDefault(Setting.IsFullScreen, false);

            OpenUICommand = new AsyncRelayCommand<TitleBarButton>(OpenUIAsync);
            LaunchCommand = new AsyncRelayCommand<string>(LaunchByOption);
            CloseUICommand = new RelayCommand(SaveAllAccounts);
            DeleteAccountCommand = new AsyncRelayCommand(DeleteAccountAsync);
        }

        private async Task OpenUIAsync(TitleBarButton? t)
        {
            string? launcherPath = settingService.GetOrDefault<string?>(Setting.LauncherPath, null);
            launcherPath = launchService.SelectLaunchDirectoryIfNull(launcherPath);
            bool result = false;
            if (launcherPath is not null && launchService.TryLoadIniData(launcherPath))
            {
                await MatchAccount();
                CurrentScheme = KnownSchemes.First(item => item.Channel == launchService.GameConfig["General"]["channel"]);
                t?.ShowAttachedFlyout<Grid>(this);
                View = t;
                result = true;
            }
            else
            {
                await App.Current.Dispatcher.InvokeAsync(new ContentDialog()
                {
                    Title = "无法使用此功能",
                    Content = "我们需要启动器的路径才能启动游戏。\n如果你已经选择了正确的文件夹但仍看到此提示，\n请尝试联系开发者。",
                    PrimaryButtonText = "确定"
                }.ShowAsync).Task.Unwrap();
                result = false;
            }
            new Event(t?.GetType(), result).TrackAs(Event.OpenTitle);
        }
        private async Task LaunchByOption(string? option)
        {
            View?.HideAttachedFlyout();
            switch (option)
            {
                case "Launcher":
                    {
                        launchService.OpenOfficialLauncher(async ex =>
                        {
                            await new ContentDialog()
                            {
                                Title = "打开启动器失败",
                                Content = ex.Message,
                                PrimaryButtonText = "确定",
                                DefaultButton = ContentDialogButton.Primary
                            }.ShowAsync();
                        });
                        break;
                    }
                case "Game":
                    {
                        await launchService.LaunchAsync(CurrentScheme, async ex =>
                        {
                            await new ContentDialog()
                            {
                                Title = "启动游戏失败",
                                Content = ex.Message,
                                PrimaryButtonText = "确定",
                                DefaultButton = ContentDialogButton.Primary
                            }.ShowAsync();
                        }, IsBorderless, IsFullScreen);
                        break;
                    }
            }
        }
        private void SaveAllAccounts()
        {
            launchService.SaveAllAccounts(Accounts);
        }
        private async Task DeleteAccountAsync()
        {
            if (SelectedAccount is not null)
            {
                if (Accounts.Count <= 1)
                {
                    View?.HideAttachedFlyout();
                    await App.Current.Dispatcher.InvokeAsync(new ContentDialog()
                    {
                        Title = "删除账户失败",
                        Content = "我们需要至少一组信息才能正常启动游戏。",
                        PrimaryButtonText = "确定"
                    }.ShowAsync).Task.Unwrap();
                    return;
                }
                Accounts.Remove(SelectedAccount);
                SelectedAccount = Accounts.Last();
            }
        }
        /// <summary>
        /// 从注册表获取当前的账户信息
        /// </summary>
        private async Task MatchAccount()
        {
            //注册表内有账号信息
            if (launchService.GetFromRegistry() is GenshinAccount currentRegistryAccount)
            {
                GenshinAccount? matched = Accounts.FirstOrDefault(
                    a => a.GeneralData == currentRegistryAccount.GeneralData && a.MihoyoSDK == currentRegistryAccount.MihoyoSDK);
                //账号列表内无匹配项
                if (matched is null)
                {
                    //命名
                    currentRegistryAccount.Name = await new NameDialog { TargetAccount = currentRegistryAccount }.GetInputAsync();
                    Accounts.Add(currentRegistryAccount);
                    selectedAccount = currentRegistryAccount;
                }
                else
                {
                    selectedAccount = matched;
                }
                //prevent registry set
                OnPropertyChanged(nameof(SelectedAccount));
            }
        }
    }
}