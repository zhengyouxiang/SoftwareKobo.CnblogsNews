﻿using System.Collections.Generic;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using GalaSoft.MvvmLight.Views;
using SoftwareKobo.CnblogsAPI.Model;
using SoftwareKobo.CnblogsAPI.Service;
using SoftwareKobo.CnblogsNews.Helper;
using SoftwareKobo.CnblogsNews.Service;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace SoftwareKobo.CnblogsNews.ViewModel
{
    public class MainPageViewModel : ViewModelBase
    {
        private int _currentPage;

        private bool _isLoading;

        private ObservableCollection<News> _newsItems;

        public MainPageViewModel()
        {
            OnCreated();
        }

        public ICommand BackCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    if (_isLoading == false)
                    {
                        CurrentPage--;
                        await GetNews();
                    }
                }, () => CurrentPage > 1);
            }
        }

        public int CurrentPage
        {
            get
            {
                return _currentPage;
            }
            set
            {
                _currentPage = value;
                RaisePropertyChanged(() => CurrentPage);
                RaisePropertyChanged(() => BackCommand);
            }
        }

        public ICommand ForwardCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    if (_isLoading == false)
                    {
                        CurrentPage++;
                        await GetNews();
                    }
                }, () => CurrentPage < 100);
            }
        }


        public ICommand JumpPageCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    var page = await JumpPageService.GetPage();
                    if (page.HasValue)
                    {
                        if (page.Value > 0 && page.Value <= 100)
                        {
                            CurrentPage = page.Value;
                            await GetNews();
                        }
                        else
                        {
                            await new DialogService().ShowMessageBox("请输入大于 0，小于等于 100 的整数。", "页码错误");
                        }
                    }
                });
            }
        }

        public ICommand NewsItemClickCommand
        {
            get
            {
                return new RelayCommand<ItemClickEventArgs>((ItemClickEventArgs e) =>
                {
                    if (e == null || e.ClickedItem == null)
                    {
                        return;
                    }
                    var news = e.ClickedItem as News;
                    if (news == null)
                    {
                        return;
                    }
                    Messenger.Default.Send<Tuple<string, News>>(new Tuple<string, News>("detail", news));
                });
            }
        }

        public ICommand ViewCommentsCommand
        {
            get
            {
                return new RelayCommand<News>(async (News news) =>
                {
                    if (news == null)
                    {
                        throw new ArgumentNullException("news");
                    }
                    var commentCount = news.Comments;
                    if (commentCount <= 0)
                    {
                        await new DialogService().ShowMessageBox("该新闻暂时还没有评论。", string.Empty);
                    }
                    else
                    {
                        Messenger.Default.Send<Tuple<string, News>>(new Tuple<string, News>("comment", news));
                    }
                });
            }
        }

        public ObservableCollection<News> NewsItems
        {
            get
            {
                return _newsItems;
            }
            set
            {
                _newsItems = value;
                RaisePropertyChanged(() => NewsItems);
            }
        }

        public ICommand RefreshCommand
        {
            get
            {
                return new RelayCommand(async () =>
                {
                    if (_isLoading == false)
                    {
                        await GetNews();
                    }
                });
            }
        }

        public async Task GetNews()
        {
            if (NetworkService.IsNetworkAvailable() == false)
            {
                await NetworkService.ShowCheckNetwork();
                return;
            }
            _isLoading = true;
            await StatusBarHelper.Display(true);
            Exception exception = null;
            try
            {
                var news = new ObservableCollection<News>(await NewsService.RecentAsync(CurrentPage, 15));
                ScrollView();
                this.NewsItems = news;
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            if (exception != null)
            {
                await new DialogService().ShowError(exception, "错误", "关闭", null);
            }
            await StatusBarHelper.Display(false);
            _isLoading = false;
        }

        public async void OnCreated()
        {
            CurrentPage = 1;
            if (IsInDesignMode == false)
            {
                await GetNews();
            }
        }

        private void ScrollView()
        {
            Messenger.Default.Send<string>("scrolltop");
        }
    }
}