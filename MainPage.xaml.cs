using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage;
using System.ComponentModel;
using Windows.System;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Diarix.UserControls;
using Diarix.ContentDialogs;
using Windows.ApplicationModel.Resources;
using System.Collections.ObjectModel;


// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace Diarix
{
    namespace Converters
    {
        public class SelectableDatesConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                return value == null ? null :
                    from date in value as ObservableCollection<DateTime>
                    orderby date.Ticks
                    select date;
            }
            public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
        }
        public class SplitViewPaneContentVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                var contentKind = (SplitViewPaneContentKind)Enum.Parse(typeof(SplitViewPaneContentKind), parameter.ToString());
                return (SplitViewPaneContentKind)value == contentKind ? Visibility.Visible : Visibility.Collapsed;
            }
            public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
        }
        public class EditBarContentVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language)
            {
                var contentKind = (EditBarContentKind)Enum.Parse(typeof(EditBarContentKind), parameter.ToString());
                return (EditBarContentKind)value == contentKind ? Visibility.Visible : Visibility.Collapsed;
            }
            public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
        }
        public class EditAvailabilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language) =>
                (bool)value ? Visibility.Visible : Visibility.Collapsed;
            public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
        }
    }
    public enum SplitViewPaneContentKind
    {
        Calendar,
        Search,
        Setting,
    }
    public enum EditBarContentKind
    {
        None,
        Paragraph,
        ImageFlipView,
        Media,
        HyperLink,
    }
    public partial class MainPage : Page, INotifyPropertyChanged
    {
        //PropertyChangedに反応するプロパティに紐づけられたフィールド
        private ObservableCollection<DateTime> _existingDiaryDates;
        private SplitViewPaneContentKind _visibleSplitViewPaneContent;
        private EditBarContentKind _visibleEditBarContent;
        private bool _isEditEnabled;
        private bool _isCompactModeEnabled;
        private bool _isSkipPasswordInput;
        private string _message;
        //フィールド
        private bool isDiaryFilesetOperationEnabled;
        private bool isSettingFilesetOperationEnabled;
        private DiaryFileset diaryFileset;
        private ObservableCollection<DiaryEntry> diaryEntries;
        private DiaryEntry selectedDiaryEntry;
        private bool hasDiaryEdited;
        private ShortcutMonitor shortcutMonitor;
        private readonly DailyDateTimeComparer dailyDateTimeComparer = new DailyDateTimeComparer();
        //PropertyChangedに反応するプロパティ
        private ObservableCollection<DateTime> ExistingDiaryDates
        {
            get => this._existingDiaryDates;
            set
            {
                this._existingDiaryDates = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ExistingDiaryDates)));
            }
        }
        private SplitViewPaneContentKind VisibleSplitViewPaneContent
        {
            get => this._visibleSplitViewPaneContent;
            set
            {
                if (this._visibleSplitViewPaneContent != value)
                {
                    this._visibleSplitViewPaneContent = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.VisibleSplitViewPaneContent)));
                }
            }
        }
        private EditBarContentKind VisibleEditBarContent
        {
            get => this._visibleEditBarContent;
            set
            {
                if (this._visibleEditBarContent != value)
                {
                    this._visibleEditBarContent = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.VisibleEditBarContent)));
                }
            }
        }
        private bool IsEditEnabled
        {
            get => this._isEditEnabled;
            set
            {
                if (this._isEditEnabled != value)
                {
                    this._isEditEnabled = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsEditEnabled)));
                }
            }
        }
        private bool IsCompactModeEnabled
        {
            get => this._isCompactModeEnabled;
            set
            {
                if (this._isCompactModeEnabled != value)
                {
                    this._isCompactModeEnabled = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsCompactModeEnabled)));
                }
            }
        }
        private bool IsSkipPasswordInput
        {
            get => this._isSkipPasswordInput;
            set
            {
                if (this._isSkipPasswordInput != value)
                {
                    this._isSkipPasswordInput = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsSkipPasswordInput)));
                }
            }
        }
        private string Message
        {
            get => this._message;
            set
            {
                this._message = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Message)));
            }
        }
        //
        public event PropertyChangedEventHandler PropertyChanged;
        public MainPage()
        {
            InitializeComponent();
            //
            this.isDiaryFilesetOperationEnabled = true;
            this.isSettingFilesetOperationEnabled = true;
            this.diaryEntries = new ObservableCollection<DiaryEntry>();
            this.hasDiaryEdited = false;
            //
            this.shortcutMonitor = new ShortcutMonitor();
            this.PreviewKeyDown += (sender, e) => this.shortcutMonitor.OnKeyDown(e.Key);
            this.PreviewKeyUp += (sender, e) => this.shortcutMonitor.OnKeyUp(e.Key);
            this.shortcutMonitor.ShortcutKeyDown += this.ShortcutMonitor_ShortcutKeyDown;
            //
            this.VisibleSplitViewPaneContent = SplitViewPaneContentKind.Calendar;
            this.VisibleEditBarContent = EditBarContentKind.None;
            this.IsEditEnabled = true;
            this.IsCompactModeEnabled = false;
            this.IsSkipPasswordInput = false;
        }
        private void OnDiaryEntryEdited(object sender, EventArgs e)
        {
            this.hasDiaryEdited = true;
        }
        private void OnDiaryEntryTapped(object sender, TappedRoutedEventArgs e)
        {
            this.ChangeDiaryEntryFocus(sender as DiaryEntry);
        }
        private void AddDiaryEntry(DiaryEntry entry)
        {
            //編集されたことを認識できるように
            entry.Edited += this.OnDiaryEntryEdited;
            entry.Tapped += this.OnDiaryEntryTapped;
            //フリップビューで選択される画像が切り替わった時に，エディットバーの画像の説明文を正しく反映するためにイベントを購読
            if (this.IsEditEnabled && entry is DiaryImageFlipView)
                (entry as DiaryImageFlipView).SelectedImageExplanationChanged += this.DiaryImageFlipView_SelectedImageExplanationChanged;
            //追加
            this.diaryEntries.Add(entry);
            this.hasDiaryEdited = true;
        }
        private async Task AddDiaryEntryAsync(DiaryEntryInformation info)
        {
            DiaryEntry entry;
            switch (info.Kind)
            {
                case DiaryEntryKind.Paragraph: entry = await DiaryParagraph.CreateDiaryParagraphAsync(info, this.IsEditEnabled, Dispatcher); break;
                case DiaryEntryKind.ImageFlipView: entry = await DiaryImageFlipView.CreateDiaryImageFlipViewAsync(this.diaryFileset, info, this.IsEditEnabled, Dispatcher); break;
                //case DiaryEntryKind.Media: entry = await DiaryMediaPlayer.CreateDiaryMediaPlayerAsync(this.diaryFileset, info, this.IsEditEnabled, Dispatcher); break;
                case DiaryEntryKind.HyperLink: entry = await DiaryHyperLink.CreateDiaryHyperLinkAsync(info, this.IsEditEnabled, Dispatcher); break;
                default: this.Message = $"Unexpected diary entry:{info.Kind} was specified."; return;
            }
            this.AddDiaryEntry(entry);
        }
        private async Task AddParagraphAsync()
        {
            if (!this.IsEditEnabled) return;
            this.AddDiaryEntry(await DiaryParagraph.CreateDiaryParagraphAsync(true, Dispatcher));
            this.Message = "段落を追加しました．";
        }
        /// <summary>
        /// 指定したフリップビューに画像を追加します．
        /// </summary>
        /// <param name="imageStorageFiles"></param>
        /// <param name="target">nullに指定した場合は新しいフリップビューを日記の末尾に追加します．</param>
        /// <returns></returns>
        private async Task AddImageFlipViewAsync(IEnumerable<IStorageFile> imageStorageFiles, DiaryImageFlipView target)
        {
            if (!this.IsEditEnabled) return;
            if (!this.isDiaryFilesetOperationEnabled) return;
            this.isDiaryFilesetOperationEnabled = false;
            var addNewEntry = target == null;
            if (addNewEntry) target = await DiaryImageFlipView.CreateDiaryImageFlipViewAsync(true, Dispatcher);
            foreach (var storage in imageStorageFiles)
            {
                try
                {
                    //画像を保存し，フリップビューに追加
                    var savedName = await this.diaryFileset.WriteStorageAsync(storage);
                    await target.AddImageFileAsync(storage, savedName);
                    //
                    this.Message = $"画像ファイル:{savedName}を追加しました．";
                }
                catch
                {
                    this.Message = "画像ファイルの追加に失敗しました．";
                    continue;
                }
            }
            //新規作成フラグが立っていれば，作成したフリップビューを日記の末尾に追加
            if (addNewEntry) this.AddDiaryEntry(target);
            this.isDiaryFilesetOperationEnabled = true;
        }
        private async Task AddResizedImagesToFlipViewAsync(IReadOnlyDictionary<string, WriteableBitmap> resizedImages)
        {
            if (!resizedImages.Any()) return;
            DiaryImageFlipView target;
            var addNewEntry = !(this.selectedDiaryEntry is DiaryImageFlipView);
            if (addNewEntry) target = await DiaryImageFlipView.CreateDiaryImageFlipViewAsync(this.IsEditEnabled, Dispatcher);
            else target = this.selectedDiaryEntry as DiaryImageFlipView;
            int savedCount = 0;
            foreach (var resizedImage in resizedImages)
            {
                var savedName = this.diaryFileset.GetIdentifiableFilename(resizedImage.Key);
                try
                {
                    var writeableBitmap = resizedImage.Value;
                    await this.diaryFileset.WriteImageAsync(savedName, writeableBitmap, true);
                    target.AddImage(savedName, writeableBitmap);
                    savedCount++;
                    this.Message = $"画像ファイル:{savedName}を保存しました．";
                }
                catch
                {
                    this.Message = $"画像ファイル:{savedName}の保存に失敗しました．";
                    continue;
                }
            }
            if (addNewEntry)
            {
                if (savedCount > 0) this.AddDiaryEntry(target);
                else target.Dispose();
            }
        }
#if false
        private async Task AddMediaAsync(IStorageFile mediaStorageFile)
        {
            if (!this.IsEditEnabled) return;
            if (!this.isDiaryFilesetOperationEnabled) return;
            this.Message = "音楽を追加しています...";
            this.isDiaryFilesetOperationEnabled = false;
            DiaryMediaPlayer player = null;
            try
            {
                player = await DiaryMediaPlayer.CreateDiaryMediaPlayerAsync(this.diaryFileset, mediaStorageFile, Dispatcher);
                this.AddDiaryEntry(player);
                this.Message = "音楽ファイルを追加しました．";
            }
            catch
            {
                player?.Dispose();
                this.Message = "音楽ファイルの追加に失敗しました．";
            }
            finally
            {
                this.isDiaryFilesetOperationEnabled = true;
            }
        }
#endif
        private async Task AddHyperLinkAsync(Uri uri)
        {
            if (!this.IsEditEnabled) return;
            this.AddDiaryEntry(await DiaryHyperLink.CreateDiaryHyperLinkAsync(uri, this.IsEditEnabled, Dispatcher));
            this.Message = "リンクを追加しました．";
        }
        private void ChangeDiaryEntryFocus(DiaryEntry newFocusedEntry)
        {
            if (!this.IsEditEnabled || newFocusedEntry == null) return;
            this.selectedDiaryEntry = newFocusedEntry;
            //エディタをロードして，選択中の要素に沿ってプロパティを設定
            switch (this.selectedDiaryEntry?.ElementKind)
            {
                case DiaryEntryKind.Paragraph:
                    this.VisibleEditBarContent = EditBarContentKind.Paragraph;
                    this.FindName(nameof(this.paragraphEditor));
                    this.paragraphEditor.ParagraphFontSize = (this.selectedDiaryEntry as DiaryParagraph).FontSize;
                    this.paragraphEditor.ParagraphFontColor = (this.selectedDiaryEntry as DiaryParagraph).ForeColor;
                    this.paragraphEditor.ParagraphBold = (this.selectedDiaryEntry as DiaryParagraph).Bold;
                    this.paragraphEditor.ParagraphItalic = (this.selectedDiaryEntry as DiaryParagraph).Italic;
                    break;
                case DiaryEntryKind.ImageFlipView:
                    this.VisibleEditBarContent = EditBarContentKind.ImageFlipView;
                    this.FindName(nameof(this.imageFlipViewEditor));
                    this.imageFlipViewEditor.ImageFlipViewWidth = (this.selectedDiaryEntry as DiaryImageFlipView).Width;
                    this.imageFlipViewEditor.ImageFlipViewHeight = (this.selectedDiaryEntry as DiaryImageFlipView).Height;
                    this.imageFlipViewEditor.ImageExplanation = (this.selectedDiaryEntry as DiaryImageFlipView).SelectedImageExplanation;
                    break;
                /*
            case DiaryEntryKind.Media:
                this.VisibleEditBarContent = EditBarContentKind.Media;
                this.FindName(nameof(this.mediaPlayerEditor));
                break;
                */
                case DiaryEntryKind.HyperLink:
                    this.VisibleEditBarContent = EditBarContentKind.HyperLink;
                    this.FindName(nameof(this.hyperLinkEditor));
                    this.hyperLinkEditor.NavigateUri = (this.selectedDiaryEntry as DiaryHyperLink).NavigateUri;
                    if (string.IsNullOrWhiteSpace(this.hyperLinkEditor.NavigateText)) this.hyperLinkEditor.NavigateText = "リンク先URL";
                    this.hyperLinkEditor.ExplanationText = (this.selectedDiaryEntry as DiaryHyperLink).Text;
                    break;
                default:
                    this.VisibleEditBarContent = EditBarContentKind.None;
                    break;
            }
        }
        private void RemoveSelectedDiaryEntry()
        {
            if (!this.IsEditEnabled || this.selectedDiaryEntry == null) return;
            //イベント購読解除
            if (this.selectedDiaryEntry is DiaryImageFlipView)
                (this.selectedDiaryEntry as DiaryImageFlipView).SelectedImageExplanationChanged -= this.DiaryImageFlipView_SelectedImageExplanationChanged;
            //
            this.selectedDiaryEntry.Dispose();
            this.diaryEntries.Remove(this.selectedDiaryEntry);
            this.selectedDiaryEntry = null;
            this.hasDiaryEdited = true;
            this.VisibleEditBarContent = EditBarContentKind.None;
        }
        /// <summary>
        /// パスワード認証ページか，画像リサイズページから飛んでくる
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null && e.Parameter.ToString() == nameof(StartPage))
            {
                try
                {
                    //パスワードを設定
                    var setting = await SettingFileset.OpenOrCreateSettingFilesetAsync();
                    //await Diarix.OldConverters.Convert.Conv(setting.DiaryFilesetKey);
                    //return;
                    this.IsCompactModeEnabled = setting.IsCompactModeEnabled;
                    this.IsSkipPasswordInput = setting.IsSkipPasswordInput;
                    this.diaryFileset = await DiaryFileset.OpenDiaryFilesetAsync(DateTime.Now, setting.DiaryFilesetKey);
                    //今日の日記を開く
                    await this.OpenDiaryAsync(this.diaryFileset.Date);
                    this.isDiaryFilesetOperationEnabled = false;
                    //日記存在リストを作成
                    var tempDates = new ObservableCollection<DateTime>(await this.diaryFileset.GetExistingAllDiaryDatetimesAsync(false));
                    if (!tempDates.Contains(DateTime.Now, this.dailyDateTimeComparer))
                    {
                        tempDates.Add(DateTime.Now);
                    }
                    this.ExistingDiaryDates = tempDates;
                    this.isDiaryFilesetOperationEnabled = true;
                }
                catch
                {
                    //ファイルの読み込みに失敗した場合は，メッセージを出し，その後のファイル操作を受け付けないようにする
                    this.Message = "日記ファイルの読み込みに失敗しました．";
                    this.isDiaryFilesetOperationEnabled = false;
                }
            }
            else if (e.Parameter is IReadOnlyDictionary<string, WriteableBitmap>)
            {
                await this.AddResizedImagesToFlipViewAsync(e.Parameter as IReadOnlyDictionary<string, WriteableBitmap>);
            }
            base.OnNavigatedTo(e);
        }
        private async Task OpenDiaryAsync(DateTime date)
        {
            if (!this.isDiaryFilesetOperationEnabled) return;
            this.isDiaryFilesetOperationEnabled = false;
            //日記を開いている途中であることを伝える
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => this.Message = $"{date.ToShortDateString()} の日記を開いています...");
            //
            this.VisibleEditBarContent = EditBarContentKind.None;
            this.IsEditEnabled = this.dailyDateTimeComparer.Equals(date, DateTime.Now);
            //this.IsEditEnabled = true;
            //以前の日記をクリア
            foreach (var entry in this.diaryEntries)
            {
                entry.Edited -= this.OnDiaryEntryEdited;
                entry.Tapped -= this.OnDiaryEntryTapped;
                entry.Dispose();
            }
            this.diaryEntries.Clear();
            //エディットバーをアンロード
            this.UnloadObject(this.paragraphEditor);
            this.UnloadObject(this.imageFlipViewEditor);
            //this.UnloadObject(this.mediaPlayerEditor);
            this.UnloadObject(this.hyperLinkEditor);
            //日記を読み込む
            try
            {
                //日記要素を追加
                var infos = DiaryEntry.GetDiaryEntryInfos(await this.diaryFileset.ReadDiaryAsync(date));
                foreach (var info in infos)
                {
                    try
                    {
                        await this.AddDiaryEntryAsync(info);
                    }
                    catch
                    {
                        continue;
                    }
                }
                //編集可能な場合で，日記要素が一つもなければパラグラフを追加
                if (this.IsEditEnabled && !this.diaryEntries.Any()) await this.AddParagraphAsync();
                //アプリ起動中に日をまたいだときのための処理
                if (this.ExistingDiaryDates != null && !this.ExistingDiaryDates.Contains(DateTime.Now, this.dailyDateTimeComparer))
                {
                    ObservableCollection<DateTime> temp = new ObservableCollection<DateTime>(this.ExistingDiaryDates)
                    {
                        DateTime.Now
                    };
                    this.ExistingDiaryDates = temp;
                }
                //
                this.Message = $"{date.ToShortDateString()} の日記を開きました．";
            }
            catch
            {
                this.Message = $"{date.ToShortDateString()} の日記を開けませんでした．";
            }
            finally
            {
                this.hasDiaryEdited = false;
                this.isDiaryFilesetOperationEnabled = true;
            }
        }
        private async Task SaveDiaryAsync()
        {
            if (!this.isDiaryFilesetOperationEnabled || !this.IsEditEnabled) return;
            this.isDiaryFilesetOperationEnabled = false;
            this.Message = $"{this.diaryFileset.Date.ToShortDateString()} の日記を保存中...";
            try
            {
                var diaryText = DiaryEntry.DiaryEntriesToString(this.diaryEntries);
                await this.diaryFileset.WriteDiaryAsync(diaryText);
                await this.diaryFileset.FlushAsync();
                //編集フラグをリセット
                this.hasDiaryEdited = false;
                //
                this.Message = $"{this.diaryFileset.Date.ToShortDateString()} の日記を保存しました．";
            }
            catch
            {
                this.Message = $"{this.diaryFileset.Date.ToShortDateString()} の日記を保存できませんでした．";
            }
            finally
            {
                this.isDiaryFilesetOperationEnabled = true;
            }
        }
        private async Task<CheckToSaveDialyDialogResult> CheckToSaveDiaryAsync(bool isCancelButtonEnabled)
        {
            CheckToSaveDiaryDialog dialog = new CheckToSaveDiaryDialog(isCancelButtonEnabled);
            return await dialog.ShowAsync();
        }
        #region events of MainPage
        private void Page_DragOver(object sender, DragEventArgs e)
        {
            if (this.IsEditEnabled)
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                e.DragUIOverride.Caption = "ここにドロップしてコンテンツを日記の末尾に追加";
            }
            else
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            }
        }
        private async void Page_Drop(object sender, DragEventArgs e)
        {
            if (!this.IsEditEnabled) return;
            //リンクを渡された場合
            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.WebLink))
            {
                var uri = await e.DataView.GetWebLinkAsync();
                await this.AddHyperLinkAsync(uri);
            }
            //ファイルを渡された場合
            else if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                /*if (items.Count == 1)
                {
                    //音楽追加の処理
                    //音楽が追加できたならreturnする
                    var mediaStorage = items.First() as IStorageFile;
                    if (DiaryFileset.SupportedMediaExtenions.Contains(mediaStorage.FileType))
                    {
                        await this.AddMediaAsync(mediaStorage as IStorageFile);
                        return;
                    }
                }
                */
                //画像ファイルのみ取り出して追加
                var imageItems =
                    from item in items
                    where DiaryFileset.SupportedImageExtensions.Contains(Path.GetExtension(item.Name).ToLower())
                    select item as IStorageFile;
                await this.AddImageFlipViewAsync(imageItems, null);
            }
        }
        private async void ShortcutMonitor_ShortcutKeyDown(object sender, ShortcutKeyDownEventAges e)
        {
            //ショートカットキー入力情報をクリア
            (sender as ShortcutMonitor).Clear();
            //まずは編集中にのみ有効なショートカットキーを調べる
            if (this.IsEditEnabled && !e.IsShiftKeyDown)
            {
                switch (e.PrimaryKey)
                {
                    case VirtualKey.S: await this.SaveDiaryAsync(); return;
                    case VirtualKey.D:
                        switch (this.selectedDiaryEntry?.ElementKind)
                        {
                            case DiaryEntryKind.Paragraph: this.paragraphEditor_ParagraphDeleted(null, null); return;
                            case DiaryEntryKind.ImageFlipView: this.imageFlipViewEditor_FlipViewDeleted(null, null); return;
                            //case DiaryEntryKind.Media: this.mediaPlayerEditor_MediaDeleted(null, null); return;
                            case DiaryEntryKind.HyperLink: this.hyperLinkEditor_HyperLinkDeleted(null, null); return;
                            default: return;
                        }
                    case VirtualKey.T: this.AddParagraphButton_Tapped(null, null); return;
                    case VirtualKey.I: this.AddImageButton_Tapped(null, null); return;
                    case VirtualKey.J: this.ResizeAndAddImageButton_Tapped(null, null); return;
                    //case VirtualKey.M: this.AddMediaButton_Tapped(null, null); return;
                    case VirtualKey.L: this.AddHyperLinkButton_Tapped(null, null); return;
                }
            }
            //編集中かどうかにかかわらず有効なショートカットキーを調べる
            switch (e.PrimaryKey)
            {
                case VirtualKey.P: this.PreviousDiaryButton_Tapped(null, null); return;
                case VirtualKey.N: this.NextDiaryButton_Tapped(null, null); return;
                case VirtualKey.H:
                    this.VisibleSplitViewPaneContent = SplitViewPaneContentKind.Calendar;
                    this.splitView.IsPaneOpen = true;
                    return;
                case VirtualKey.F:
                    this.VisibleSplitViewPaneContent = SplitViewPaneContentKind.Search;
                    this.splitView.IsPaneOpen = true;
                    return;
            }
        }
        #endregion
        #region events to move to splitview.pane
        private void CalendarToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            this.VisibleSplitViewPaneContent = SplitViewPaneContentKind.Calendar;
            this.splitView.IsPaneOpen = true;
            (sender as ToggleButton).IsChecked = false;
        }
        private void SearchToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            this.VisibleSplitViewPaneContent = SplitViewPaneContentKind.Search;
            this.splitView.IsPaneOpen = true;
            (sender as ToggleButton).IsChecked = false;
        }
        private void SettingToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            this.VisibleSplitViewPaneContent = SplitViewPaneContentKind.Setting;
            this.splitView.IsPaneOpen = true;
            (sender as ToggleButton).IsChecked = false;
        }
        #endregion
        #region events of controls in splitview.pane
        private async void HistoryCalendar_DateSelected(object sender, DateTime e)
        {
            if (this.hasDiaryEdited)
            {
                switch (await this.CheckToSaveDiaryAsync(true))
                {
                    case CheckToSaveDialyDialogResult.Save: await this.SaveDiaryAsync(); break;
                    case CheckToSaveDialyDialogResult.DontSave: break;
                    default: return;
                }
            }
            await this.OpenDiaryAsync(e);
        }
        private async void DiarySearcher_SearchButtonTapped(object sender, Tuple<IEnumerable<string>, DiarySearchOption> e)
        {
            //検索する前に日記の内容を保存しておく
            if (this.hasDiaryEdited)
            {
                switch (await this.CheckToSaveDiaryAsync(false))
                {
                    case CheckToSaveDialyDialogResult.Save: await this.SaveDiaryAsync(); break;
                    default: (sender as DiarySearcher).SearchResultExplanation = "検索を中止しました．"; return;
                }
            }
            //
            if (!this.isDiaryFilesetOperationEnabled) return;
            this.isDiaryFilesetOperationEnabled = false;
            (sender as DiarySearcher).SearchResultExplanation = "検索中...";
            //検索操作前に開いていた日記の日付を保存
            var initialDiaryDate = this.diaryFileset.Date;
            //各日記のテキスト部分を取得
            Dictionary<DateTime, IEnumerable<string>> dictionary = new Dictionary<DateTime, IEnumerable<string>>();
            foreach (var date in this.ExistingDiaryDates)
            {
                try
                {
                    dictionary.Add(date, DiaryEntry.GetDiaryTexts(await this.diaryFileset.ReadDiaryAsync(date)));
                }
                catch
                {
                    continue;
                }
            }
            //検索方法を取得
            var (keywords, option) = e;
            //指定したキーワードを含む日記の日付を検索
            List<DateTime> matchDates = new List<DateTime>();
            foreach (var dailyParagraphs in dictionary)
            {
                switch (option)
                {
                    case DiarySearchOption.AndSearch:
                        if (keywords.All(keyword => dailyParagraphs.Value.Any(paragraph => paragraph.Contains(keyword))))
                            matchDates.Add(dailyParagraphs.Key);
                        break;
                    case DiarySearchOption.OrSearch:
                        if (keywords.Any(keyword => dailyParagraphs.Value.Any(paragraph => paragraph.Contains(keyword))))
                            matchDates.Add(dailyParagraphs.Key);
                        break;
                    default: this.Message = $"Unexpected option:{option}"; return;
                }
            }
            //検索条件に合致する日記のうち，キーワードの周りの文字列を取得してリストビューに追加していく
            (sender as DiarySearcher).KeywordSurrounding.Clear();
            foreach (var date in matchDates)
            {
                var surrounding = new KeywordSurrounding(dictionary[date], keywords, 12, date);
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, new Windows.UI.Core.DispatchedHandler(() =>
                 (sender as DiarySearcher).KeywordSurrounding.Add(surrounding)));
            }
            //検索操作前に開いていた日付の日記を開く
            await this.diaryFileset.ReadDiaryAsync(initialDiaryDate);
            //日記データへの操作を有効にする
            this.isDiaryFilesetOperationEnabled = true;
        }
        private async void DiarySearcher_SearchResultTapped(object sender, DateTime e)
        {
            if (this.hasDiaryEdited)
            {
                switch (await this.CheckToSaveDiaryAsync(true))
                {
                    case CheckToSaveDialyDialogResult.Save: await this.SaveDiaryAsync(); break;
                    case CheckToSaveDialyDialogResult.DontSave: break;
                    default: return;
                }
            }
            await this.OpenDiaryAsync(e);
        }
        private async void SettingManager_CompactButtonToggled(object sender, bool e)
        {
            this.IsCompactModeEnabled = e;
            if (!this.isSettingFilesetOperationEnabled) return;
            this.isSettingFilesetOperationEnabled = false;
            try
            {
                var setting = await SettingFileset.OpenOrCreateSettingFilesetAsync();
                setting.IsCompactModeEnabled = this.IsCompactModeEnabled;
                await setting.FlushAsync();
                this.Message = "設定を保存しました．";
            }
            catch
            {
                this.Message = "設定の変更に失敗しました．";
            }
            finally
            {
                this.isSettingFilesetOperationEnabled = true;
            }
        }
        private async void SettingManager_PasswordSkipToggled(object sender, bool e)
        {
            this.IsSkipPasswordInput = e;
            if (!this.isSettingFilesetOperationEnabled) return;
            this.isSettingFilesetOperationEnabled = false;
            try
            {
                var setting = await SettingFileset.OpenOrCreateSettingFilesetAsync();
                setting.IsSkipPasswordInput = this.IsSkipPasswordInput;
                await setting.FlushAsync();
                this.Message = "設定を保存しました．";
            }
            catch
            {
                this.Message = "設定の変更に失敗しました．";
            }
            finally
            {
                this.isSettingFilesetOperationEnabled = true;
            }
        }
        private async void SettingManager_NewPasswordDetermined(object sender, string e)
        {
            if (!this.isDiaryFilesetOperationEnabled) return;
            this.isDiaryFilesetOperationEnabled = false;
            //パスワード変更
            try
            {
                this.Message = "パスワードを変更しています...";
                var setting = await SettingFileset.OpenOrCreateSettingFilesetAsync();
                setting.SetPassword(e);
                await setting.FlushAsync();
                await this.diaryFileset.ChangeKeyAsync(setting.DiaryFilesetKey);
                this.Message = "パスワードの変更が完了しました．";
            }
            catch
            {
                this.Message = "パスワードの変更に失敗しました．";
            }
            finally
            {
                this.isDiaryFilesetOperationEnabled = true;
            }
        }
        #endregion
        #region events of editors
        private void paragraphEditor_ParagraphFontSizeChanged(object sender, int e)
        {
            var selectedParagraph = this.selectedDiaryEntry as DiaryParagraph;
            if (selectedParagraph == null) return;
            selectedParagraph.FontSize = e;
        }
        private void paragraphEditor_ParagraphFontColorChanged(object sender, Color e)
        {
            var selectedParagraph = this.selectedDiaryEntry as DiaryParagraph;
            if (selectedParagraph == null) return;
            selectedParagraph.ForeColor = e;
        }
        private void paragraphEditor_ParagraphBoldChanged(object sender, bool e)
        {
            var selectedParagraph = this.selectedDiaryEntry as DiaryParagraph;
            if (selectedParagraph == null) return;
            selectedParagraph.Bold = e;
        }
        private void paragraphEditor_ParagraphItalicChanged(object sender, bool e)
        {
            var selectedParagraph = this.selectedDiaryEntry as DiaryParagraph;
            if (selectedParagraph == null) return;
            selectedParagraph.Italic = e;
        }
        private void paragraphEditor_ParagraphDeleted(object sender, EventArgs e)
        {
            var selectedParagraph = this.selectedDiaryEntry as DiaryParagraph;
            if (selectedParagraph == null) return;
            this.RemoveSelectedDiaryEntry();
            this.Message = "段落を削除しました．";
        }
        private void imageFlipViewEditor_FlipViewWidthChanged(object sender, int e)
        {
            var selectedImageFlipView = this.selectedDiaryEntry as DiaryImageFlipView;
            if (selectedImageFlipView == null) return;
            selectedImageFlipView.Width = e;
        }
        private void imageFlipViewEditor_FlipViewHeightChanged(object sender, int e)
        {
            var selectedImageFlipView = this.selectedDiaryEntry as DiaryImageFlipView;
            if (selectedImageFlipView == null) return;
            selectedImageFlipView.Height = e;
        }
        private async void imageFlipViewEditor_ImageAdded(object sender, IEnumerable<IStorageFile> e)
        {
            var selectedImageFlipView = this.selectedDiaryEntry as DiaryImageFlipView;
            if (selectedImageFlipView == null) return;
            await this.AddImageFlipViewAsync(e, selectedImageFlipView);
        }
        private void imageFlipViewEditor_ImageResizedAndAdded(object sender, IEnumerable<IStorageFile> e)
        {
            var selectedImageFlipView = this.selectedDiaryEntry as DiaryImageFlipView;
            if (selectedImageFlipView == null) return;
            this.Frame.Navigate(typeof(Pages.ImageResizer), e);
        }
        private void imageFlipViewEditor_ImageDeleted(object sender, EventArgs e)
        {
            var selectedImageFlipView = this.selectedDiaryEntry as DiaryImageFlipView;
            if (selectedImageFlipView == null) return;
            if (!this.isDiaryFilesetOperationEnabled) return;
            this.isDiaryFilesetOperationEnabled = false;
            var removedImagename = selectedImageFlipView.RemoveSelectedImage();
            if (removedImagename == null) return;
            this.diaryFileset.Remove(removedImagename);
            this.Message = "選択中の画像を削除しました．";
            this.isDiaryFilesetOperationEnabled = true;
        }
        private void imageFlipViewEditor_ImageExplanationChanged(object sender, string e)
        {
            if (this.selectedDiaryEntry is DiaryImageFlipView)
            {
                (this.selectedDiaryEntry as DiaryImageFlipView).SelectedImageExplanation = e;
            }
        }
        private void imageFlipViewEditor_FlipViewDeleted(object sender, EventArgs e)
        {
            var selectedImageFlipView = this.selectedDiaryEntry as DiaryImageFlipView;
            if (selectedImageFlipView == null) return;
            if (!this.isDiaryFilesetOperationEnabled) return;
            this.isDiaryFilesetOperationEnabled = false;
            var removedImageNames = selectedImageFlipView.RemoveAllImages();
            foreach (var removedImageName in removedImageNames) this.diaryFileset.Remove(removedImageName);
            this.RemoveSelectedDiaryEntry();
            this.Message = "フリップビューを削除しました．";
            this.isDiaryFilesetOperationEnabled = true;
        }
#if false
        private void mediaPlayerEditor_MediaDeleted(object sender, EventArgs e)
        {
            var selectedPlayer = this.selectedDiaryEntry as DiaryMediaPlayer;
            if (selectedPlayer == null) return;
            if (!this.isDiaryFilesetOperationEnabled) return;
            this.isDiaryFilesetOperationEnabled = false;
            this.diaryFileset.Remove(selectedPlayer.SourceFilename);
            this.RemoveSelectedDiaryEntry();
            this.Message = "音楽を削除しました．";
            this.isDiaryFilesetOperationEnabled = true;
        }
#endif
        private void hyperLinkEditor_NavigateUriChanged(object sender, Uri e)
        {
            var selectedHyperLink = this.selectedDiaryEntry as DiaryHyperLink;
            if (selectedHyperLink == null) return;
            selectedHyperLink.NavigateUri = e;
        }
        private void hyperLinkEditor_ExplanationTextChanged(object sender, string e)
        {
            var selectedHyperLink = this.selectedDiaryEntry as DiaryHyperLink;
            if (selectedHyperLink == null) return;
            selectedHyperLink.Text = e;
        }
        private void hyperLinkEditor_HyperLinkDeleted(object sender, EventArgs e)
        {
            var selectedHyperLink = this.selectedDiaryEntry as DiaryHyperLink;
            if (selectedHyperLink == null) return;
            this.RemoveSelectedDiaryEntry();
            this.Message = "リンクを削除しました．";
        }
        #endregion
#if false
        private async void AddMediaButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.IsEditEnabled) return;
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                CommitButtonText = "追加",
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
            };
            foreach (var extension in DiaryFileset.SupportedMediaExtenions) picker.FileTypeFilter.Add(extension);
            var result = await picker.PickSingleFileAsync();
            //ファイル選択がキャンセルされたら何もしない
            if (result != null) await this.AddMediaAsync(result);
        }
#endif
        #region events of diary entries
        private void DiaryImageFlipView_SelectedImageExplanationChanged(object sender, string e)
        {
            if (this.imageFlipViewEditor != null) this.imageFlipViewEditor.ImageExplanation = e;
        }
        #endregion
        private async void SaveButton_Tapped(object sender, TappedRoutedEventArgs e) => await this.SaveDiaryAsync();
        private async void SaveButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter) await this.SaveDiaryAsync();
        }
        private async void AddParagraphButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.IsEditEnabled) return;
            await this.AddParagraphAsync();
        }
        private async void AddParagraphButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!this.IsEditEnabled || e.Key != VirtualKey.Enter) return;
            await this.AddParagraphAsync();
        }
        private async void AddImageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.IsEditEnabled) return;
            //ピッカーを作成
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                CommitButtonText = "追加",
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var extension in DiaryFileset.SupportedImageExtensions) picker.FileTypeFilter.Add(extension);
            //日記に追加する画像を選ばせる
            var results = await picker.PickMultipleFilesAsync();
            //日記に追加
            if (results != null && results.Any())
            {
                await this.AddImageFlipViewAsync(results, null);
            }
        }
        private async void AddImageButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!this.IsEditEnabled || e.Key != VirtualKey.Enter) return;
            //ピッカーを作成
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                CommitButtonText = "追加",
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var extension in DiaryFileset.SupportedImageExtensions) picker.FileTypeFilter.Add(extension);
            //日記に追加する画像を選ばせる
            var results = await picker.PickMultipleFilesAsync();
            //日記に追加
            if (results != null && results.Any())
            {
                await this.AddImageFlipViewAsync(results, null);
            }
        }
        private async void ResizeAndAddImageButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.IsEditEnabled) return;
            //ピッカーを作成
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                CommitButtonText = "追加",
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var extension in DiaryFileset.SupportedImageExtensions)
            {
                if (extension == ".gif") continue;
                picker.FileTypeFilter.Add(extension);
            }
            //日記に追加する画像を選ばせる
            var results = await picker.PickMultipleFilesAsync();
            //日記に追加
            if (results != null && results.Any())
            {
                //await this.AddImageFlipViewWithResizeAsync(results, null);
                this.Frame.Navigate(typeof(Pages.ImageResizer), results);
            }
        }
        private async void ResizeAndAddImageButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!this.IsEditEnabled || e.Key != VirtualKey.Enter) return;
            //ピッカーを作成
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                CommitButtonText = "追加",
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var extension in DiaryFileset.SupportedImageExtensions)
            {
                if (extension == ".gif") continue;
                picker.FileTypeFilter.Add(extension);
            }
            //日記に追加する画像を選ばせる
            var results = await picker.PickMultipleFilesAsync();
            //日記に追加
            if (results != null && results.Any())
            {
                //await this.AddImageFlipViewWithResizeAsync(results, null);
                this.Frame.Navigate(typeof(Pages.ImageResizer), results);
            }
        }
        private async void AddHyperLinkButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.IsEditEnabled) return;
            await this.AddHyperLinkAsync(null);
        }
        private async void AddHyperLinkButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!this.IsEditEnabled || e.Key != VirtualKey.Enter) return;
            await this.AddHyperLinkAsync(null);
        }
        private async void PreviousDiaryButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.isDiaryFilesetOperationEnabled) return;
            if (this.dailyDateTimeComparer.Equals(this.diaryFileset.Date, this.ExistingDiaryDates.First()))
            {
                this.Message = "これが最も昔の日記です．";
                return;
            }
            if (this.hasDiaryEdited)
            {
                switch (await this.CheckToSaveDiaryAsync(true))
                {
                    case CheckToSaveDialyDialogResult.Save: await this.SaveDiaryAsync(); break;
                    case CheckToSaveDialyDialogResult.DontSave: break;
                    default: return;
                }
            }
            var index = this.ExistingDiaryDates.IndexOf(this.diaryFileset.Date, this.dailyDateTimeComparer) - 1;
            await this.OpenDiaryAsync(this.ExistingDiaryDates.ElementAt(index));
        }
        private async void PreviousDiaryButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!this.isDiaryFilesetOperationEnabled || e.Key != VirtualKey.Enter) return;
            if (this.dailyDateTimeComparer.Equals(this.diaryFileset.Date, this.ExistingDiaryDates.First()))
            {
                this.Message = "これが最も昔の日記です．";
                return;
            }
            if (this.hasDiaryEdited)
            {
                switch (await this.CheckToSaveDiaryAsync(true))
                {
                    case CheckToSaveDialyDialogResult.Save: await this.SaveDiaryAsync(); break;
                    case CheckToSaveDialyDialogResult.DontSave: break;
                    default: return;
                }
            }
            var index = this.ExistingDiaryDates.IndexOf(this.diaryFileset.Date, this.dailyDateTimeComparer) - 1;
            await this.OpenDiaryAsync(this.ExistingDiaryDates.ElementAt(index));
        }
        private async void NextDiaryButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.isDiaryFilesetOperationEnabled) return;
            if (this.dailyDateTimeComparer.Equals(this.diaryFileset.Date, this.ExistingDiaryDates.Last()))
            {
                this.Message = "これが最新の日記です．";
                return;
            }
            if (this.hasDiaryEdited)
            {
                switch (await this.CheckToSaveDiaryAsync(true))
                {
                    case CheckToSaveDialyDialogResult.Save: await this.SaveDiaryAsync(); break;
                    case CheckToSaveDialyDialogResult.DontSave: break;
                    default: return;
                }
            }
            var index = this.ExistingDiaryDates.IndexOf(this.diaryFileset.Date, this.dailyDateTimeComparer) + 1;
            await this.OpenDiaryAsync(this.ExistingDiaryDates.ElementAt(index));
        }
        private async void NextDiaryButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!this.isDiaryFilesetOperationEnabled || e.Key != VirtualKey.Enter) return;
            if (this.dailyDateTimeComparer.Equals(this.diaryFileset.Date, this.ExistingDiaryDates.Last()))
            {
                this.Message = "これが最新の日記です．";
                return;
            }
            if (this.hasDiaryEdited)
            {
                switch (await this.CheckToSaveDiaryAsync(true))
                {
                    case CheckToSaveDialyDialogResult.Save: await this.SaveDiaryAsync(); break;
                    case CheckToSaveDialyDialogResult.DontSave: break;
                    default: return;
                }
            }
            var index = this.ExistingDiaryDates.IndexOf(this.diaryFileset.Date, this.dailyDateTimeComparer) + 1;
            await this.OpenDiaryAsync(this.ExistingDiaryDates.ElementAt(index));
        }
    }
}
