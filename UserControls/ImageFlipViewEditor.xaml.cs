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
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using Windows.Storage;
using Windows.Storage.Pickers;

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Diarix.UserControls
{
    namespace Converters
    {
        public class SizeToStringConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, string language) => value.ToString();
            public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
        }
    }
    public sealed partial class ImageFlipViewEditor : UserControl, INotifyPropertyChanged
    {
        private bool _isCompactModeEnabled;
        private int _imageFlipViewWidth;
        private int _imageFlipViewHeight;
        private string _imageExplanation;
        public bool IsCompactModeEnabled
        {
            get => this._isCompactModeEnabled;
            set
            {
                this._isCompactModeEnabled = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.IsCompactModeEnabled)));
            }
        }
        public int ImageFlipViewWidth
        {
            get => this._imageFlipViewWidth;
            set
            {
                if (this._imageFlipViewWidth != value)
                {
                    this._imageFlipViewWidth = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ImageFlipViewWidth)));
                    this.FlipViewWidthChanged?.Invoke(this, this.ImageFlipViewWidth);
                }
            }
        }
        public int ImageFlipViewHeight
        {
            get => this._imageFlipViewHeight;
            set
            {
                if (this._imageFlipViewHeight != value)
                {
                    this._imageFlipViewHeight = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ImageFlipViewHeight)));
                    this.FlipViewHeightChanged?.Invoke(this, this.ImageFlipViewHeight);
                }
            }
        }
        public string ImageExplanation
        {
            get => this._imageExplanation;
            set
            {
                if (this._imageExplanation != value)
                {
                    this._imageExplanation = value;
                    //使用禁止文字を弾く
                    this._imageExplanation = this._imageExplanation?.Trim(DiaryEntry.ProhibitedAttributeValueChar);
                    //
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ImageExplanation)));
                    this.ImageExplanationChanged?.Invoke(this, this._imageExplanation);
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<int> FlipViewWidthChanged;
        public event EventHandler<int> FlipViewHeightChanged;
        public event EventHandler<string> ImageExplanationChanged;
        /// <summary>
        /// ユーザが画像ファイルの追加を要求したときに発生します．
        /// </summary>
        public event EventHandler<IEnumerable<IStorageFile>> ImageAdded;
        /// <summary>
        /// ユーザが画像ファイルをリサイズして追加することを要求したときに発生します．
        /// </summary>
        public event EventHandler<IEnumerable<IStorageFile>> ImageResizedAndAdded;
        /// <summary>
        /// ユーザが現在選択されている画像の削除を要求したときに発生します．
        /// </summary>
        public event EventHandler ImageDeleted;
        /// <summary>
        /// 現在捜査の対象となっているスライドショーの削除が要求されたときに発生します．
        /// </summary>
        public event EventHandler FlipViewDeleted;
        public ImageFlipViewEditor()
        {
            this.InitializeComponent();
            this.ImageFlipViewWidth = Convert.ToInt32(Application.Current.Resources["ImageFlipViewSizeDefault"]);
            this.ImageFlipViewHeight = Convert.ToInt32(Application.Current.Resources["ImageFlipViewSizeDefault"]);
        }
        private async void AddButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var extension in Diarix.DiaryFileset.SupportedImageExtensions) picker.FileTypeFilter.Add(extension);
            var results = await picker.PickMultipleFilesAsync();
            //ファイル選択がキャンセルされたら何もしない
            if (results != null && results.Any()) this.ImageAdded?.Invoke(this, results);
        }
        private async void AddButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var extension in Diarix.DiaryFileset.SupportedImageExtensions) picker.FileTypeFilter.Add(extension);
            var results = await picker.PickMultipleFilesAsync();
            //ファイル選択がキャンセルされたら何もしない
            if (results != null && results.Any()) this.ImageAdded?.Invoke(this, results);
        }
        private async void ResizeAndAddButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var extension in DiaryFileset.SupportedImageExtensions)
            {
                if (extension == ".gif") continue;
                picker.FileTypeFilter.Add(extension);
            }
            var results = await picker.PickMultipleFilesAsync();
            //ファイル選択がキャンセルされたら何もしない
            if (results != null && results.Any()) this.ImageResizedAndAdded?.Invoke(this, results);
        }
        private async void ResizeAndAddButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter) return;
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            foreach (var extension in DiaryFileset.SupportedImageExtensions)
            {
                if (extension == ".gif") continue;
                picker.FileTypeFilter.Add(extension);
            }
            var results = await picker.PickMultipleFilesAsync();
            //ファイル選択がキャンセルされたら何もしない
            if (results != null && results.Any()) this.ImageResizedAndAdded?.Invoke(this, results);
        }
        private void DeleteButton_Tapped(object sender, TappedRoutedEventArgs e) => this.ImageDeleted?.Invoke(this, null);
        private void DeleteButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.ImageDeleted?.Invoke(this, null);
        }
        private void DeleteAllButton_Tapped(object sender, TappedRoutedEventArgs e) => this.FlipViewDeleted?.Invoke(this, null);
        private void DeleteAllButton_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) this.FlipViewDeleted?.Invoke(this, null);
        }
    }
}
