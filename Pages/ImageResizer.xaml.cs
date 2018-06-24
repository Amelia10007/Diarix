using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Graphics.Imaging;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace Diarix.Pages
{
    struct IntPoint
    {
        public int X;
        public int Y;
        public IntPoint(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }
        public IntPoint(Point point)
        {
            this.X = (int)point.X;
            this.Y = (int)point.Y;
        }
        public bool IsDefault() => this.X == 0 && this.Y == 0;
    }
    class ResizedImage : INotifyPropertyChanged
    {
        private IntPoint _selectStartPoint;
        private IntPoint _selectEndPoint;
        private int _reduction;
        private SolidColorBrush _borderBrush;
        private bool isSelecting;
        public string ImageName { get; private set; }
        public BitmapImage Source { get; private set; }
        /// <summary>
        /// 画像の縮小およびトリミングに使用するデコーダ．
        /// </summary>
        private BitmapDecoder decoder;
        public int SourceWidth => (int)this.decoder.PixelWidth;
        public int SourceHeight => (int)this.decoder.PixelHeight;
        public int ReductedWidth => (int)(this.SourceWidth * this.Reduction / 100.0);
        public int ReductedHeight => (int)(this.SourceHeight * this.Reduction / 100.0);
        public IntPoint SelectStartPoint
        {
            get => this._selectStartPoint;
            set
            {
                this._selectStartPoint = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectStartPoint)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectBorderMargin)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectedWidth)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectedHeight)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Message)));
            }
        }
        public IntPoint SelectEndPoint
        {
            get => this._selectEndPoint;
            set
            {
                this._selectEndPoint = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectEndPoint)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectBorderMargin)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectedWidth)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectedHeight)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Message)));
            }
        }
        public int Reduction
        {
            get => this._reduction;
            set
            {
                this._reduction = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Reduction)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ReductedWidth)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ReductedHeight)));
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Message)));
            }
        }
        public SolidColorBrush borderBrush
        {
            get => this._borderBrush;
            set
            {
                this._borderBrush = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.borderBrush)));
            }
        }
        public Thickness SelectBorderMargin =>
            new Thickness(Math.Min(this.SelectStartPoint.X, this.SelectEndPoint.X), Math.Min(this.SelectStartPoint.Y, this.SelectEndPoint.Y), 0, 0);
        public int SelectedWidth => Math.Abs(this.SelectStartPoint.X - this.SelectEndPoint.X);
        public int SelectedHeight => Math.Abs(this.SelectStartPoint.Y - this.SelectEndPoint.Y);
        public string Message
        {
            get
            {
                var s1 = $"選択中のファイル:{this.ImageName} 元のサイズ:{this.SourceWidth}×{this.SourceHeight}\n";
                var s2 = $"拡大率:{this.Reduction}% 選択中の領域サイズ:";
                var s3 = (this.SelectedWidth + this.SelectedHeight == 0) ? $"{this.ReductedWidth}×{this.ReductedHeight}" : $"{this.SelectedWidth}×{this.SelectedHeight}";
                return s1 + s2 + s3;
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private ResizedImage()
        {
            this.isSelecting = false;
            this.SelectStartPoint = new IntPoint();
            this.SelectEndPoint = new IntPoint();
            this.Reduction = 100;
            this.borderBrush = new SolidColorBrush(Color.FromArgb(byte.MaxValue, 0, 0, 0));
        }
        public static async Task<ResizedImage> CreateResizedImage(IStorageFile imageStorageFile)
        {
            ResizedImage resizedImage = new ResizedImage()
            {
                ImageName = imageStorageFile.Name,
            };
            using (var stream = await imageStorageFile.OpenStreamForReadAsync())
            {
                using (var istream = stream.AsRandomAccessStream())
                {
                    resizedImage.Source = new BitmapImage();
                    await resizedImage.Source.SetSourceAsync(istream);
                    istream.Seek(0);
                    resizedImage.decoder = await BitmapDecoder.CreateAsync(istream);
                }
            }
            return resizedImage;
        }
        public void SelectionPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.isSelecting = true;
            var pointer = new IntPoint(e.GetCurrentPoint(sender as UIElement).Position);
            this.SelectStartPoint = pointer;
            this.SelectEndPoint = this.SelectStartPoint;
            e.Handled = true;
        }
        public void SelectionPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!this.isSelecting) return;
            var pointer = new IntPoint(e.GetCurrentPoint(sender as UIElement).Position);
            this.SelectEndPoint = pointer;
            e.Handled = true;
        }
        public void SelectionPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!this.isSelecting) return;
            this.isSelecting = false;
            this.SelectEndPoint = new IntPoint(e.GetCurrentPoint(sender as UIElement).Position);
            e.Handled = true;
        }
        public async Task<WriteableBitmap> GetResizedBitmap()
        {
            //領域を指定されなかった場合は全面を保存
            if (this.SelectStartPoint.IsDefault() && this.SelectEndPoint.IsDefault())
            {
                this.SelectEndPoint = new IntPoint(this.ReductedWidth, this.ReductedHeight);
            }
            //指定された領域の幅か高さが0の場合は何もしない
            if (this.SelectedWidth == 0 || this.SelectedHeight == 0) return null;
            //切り出す領域を選択．切り出し領域が画像の領域からはみ出ないようにする
            BitmapBounds bounds = new BitmapBounds()
            {
                X = (uint)Math.Max(this.SelectBorderMargin.Left, 0),
                Y = (uint)Math.Max(this.SelectBorderMargin.Top, 0),
            };
            //もし，始点が画像の領域外だったら何もしない
            if (this.ReductedWidth - bounds.X <= 0 || this.ReductedHeight - bounds.Y <= 0) return null;
            bounds.Width = (uint)Math.Min(this.SelectedWidth, this.ReductedWidth - bounds.X);
            bounds.Height = (uint)Math.Min(this.SelectedHeight, this.ReductedHeight - bounds.Y);
            //もし，始点が画像の領域外だったら何もしない
            if (bounds.X >= this.ReductedWidth || bounds.Y >= this.ReductedHeight) return null;
            //もし，補正後の選択領域の幅または高さが0になったら何もしない
            if (bounds.Width == 0 || bounds.Height == 0) return null;
            //
            BitmapTransform transform = new BitmapTransform()
            {
                Bounds = bounds,
                ScaledWidth = (uint)this.ReductedWidth,
                ScaledHeight = (uint)this.ReductedHeight,
            };
            var provider = await this.decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight, transform, ExifOrientationMode.IgnoreExifOrientation, ColorManagementMode.DoNotColorManage);
            var pixels = provider.DetachPixelData();
            WriteableBitmap writeableBitmap = new WriteableBitmap((int)bounds.Width, (int)bounds.Height);
            using (var stream = writeableBitmap.PixelBuffer.AsStream())
            {
                await stream.WriteAsync(pixels, 0, pixels.Length);
            }
            return writeableBitmap;
        }
    }
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class ImageResizer : Page, INotifyPropertyChanged
    {
        private ObservableCollection<ResizedImage> _resizedImages;
        private int _reductionMaximum;
        private string _message;
        private ObservableCollection<ResizedImage> resizedImages
        {
            get => this._resizedImages;
            set
            {
                this._resizedImages?.Clear();
                this._resizedImages = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.resizedImages)));
            }
        }
        private int reduction
        {
            get => (this.imageSelectFlipView.SelectedItem as ResizedImage)?.Reduction ?? 100;
            set
            {
                if (this.imageSelectFlipView.SelectedItem as ResizedImage != null && (this.imageSelectFlipView.SelectedItem as ResizedImage).Reduction != value)
                {
                    (this.imageSelectFlipView.SelectedItem as ResizedImage).Reduction = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.reduction)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isIncreaseButtonEnabled)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isDecreaseButtonEnabled)));
                }
            }
        }
        private int reductionMaximum
        {
            get => this._reductionMaximum;
            set
            {
                if (this._reductionMaximum != value)
                {
                    this._reductionMaximum = value;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.reductionMaximum)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isIncreaseButtonEnabled)));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isDecreaseButtonEnabled)));
                }
            }
        }
        private bool isIncreaseButtonEnabled => (this.imageSelectFlipView.SelectedItem as ResizedImage)?.Reduction < this.reductionMaximum;
        private bool isDecreaseButtonEnabled => (this.imageSelectFlipView.SelectedItem as ResizedImage)?.Reduction > 1;
        private bool isNextButtonEnabled => this.imageSelectFlipView.SelectedIndex < this.resizedImages.Count - 1;
        private bool isPreviousButtonEnabled => this.imageSelectFlipView.SelectedIndex > 0;
        private string message
        {
            get => this._message;
            set
            {
                this._message = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.message)));
            }
        }
        private SolidColorBrush borderBrush => (this.imageSelectFlipView.SelectedItem as ResizedImage)?.borderBrush;
        public event PropertyChangedEventHandler PropertyChanged;
        public ImageResizer()
        {
            this.InitializeComponent();
            this.resizedImages = new ObservableCollection<ResizedImage>();
            this.reductionMaximum = 100;
        }
        private void ClearResizedImages()
        {
            foreach (var resizedImage in this.resizedImages)
            {
                resizedImage.PropertyChanged -= this.OnResizedImagePropertyChanged;
            }
            this.resizedImages.Clear();
        }
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            foreach (var imageFile in e.Parameter as IEnumerable<IStorageFile>)
            {
                var resizedImage = await ResizedImage.CreateResizedImage(imageFile);
                resizedImage.PropertyChanged += this.OnResizedImagePropertyChanged;
                this.resizedImages.Add(resizedImage);
            }
            //イベントを起こしてUIの状態を整える
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.reductionMaximum)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isIncreaseButtonEnabled)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isDecreaseButtonEnabled)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isNextButtonEnabled)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isPreviousButtonEnabled)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.borderBrush)));
            //
            base.OnNavigatedTo(e);
        }
        private void OnResizedImagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var resizedImage = sender as ResizedImage;
            switch (e.PropertyName)
            {
                case nameof(resizedImage.Message): this.message = resizedImage.Message; break;
                case nameof(resizedImage.borderBrush): this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.borderBrush))); break;
            }
        }
        private void imageSelectFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (sender as FlipView).SelectedItem as ResizedImage;
            if (selectedItem == null) return;
            var newSelectedResizedImage = e.AddedItems.First() as ResizedImage;
            //元の画像が大きすぎる場合は拡大率に制限を設けて，フリップビューよりも大きくならないようにする
            var widthRatio = newSelectedResizedImage.SourceWidth / (sender as FlipView).ActualWidth;
            var heightRatio = newSelectedResizedImage.SourceHeight / (sender as FlipView).ActualHeight;
            var largerRatio = Math.Max(widthRatio, heightRatio);
            if (largerRatio > 1) this.reductionMaximum = (int)(100 / largerRatio);
            else this.reductionMaximum = 100;
            if (this.reduction > this.reductionMaximum) this.reduction = this.reductionMaximum;
            if (selectedItem.Reduction > this.reductionMaximum) selectedItem.Reduction = this.reductionMaximum;
            this.reduction = selectedItem.Reduction;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.reduction)));
            //表示に使うプロパティを更新
            this.message = selectedItem.Message;
            //
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isNextButtonEnabled)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.isPreviousButtonEnabled)));
        }
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid == null || grid.Children == null || !grid.Children.Any()) return;
            var image = grid.Children.First() as Image;
            if (image == null) return;
            image.Width = grid.Width;
            image.Height = grid.Height;
            //元の画像が大きすぎる場合は拡大率に制限を設けて，フリップビューよりも大きくならないようにする
            if (this.imageSelectFlipView.SelectedItem != null)
            {
                var selectedResizedImage = this.imageSelectFlipView.SelectedItem as ResizedImage;
                var widthRatio = selectedResizedImage.SourceWidth / this.imageSelectFlipView.ActualWidth;
                var heightRatio = selectedResizedImage.SourceHeight / this.imageSelectFlipView.ActualHeight;
                var largerRatio = Math.Max(widthRatio, heightRatio);
                if (largerRatio > 1) this.reductionMaximum = (int)(100 / largerRatio);
                else this.reductionMaximum = 100;
                if (this.reduction > this.reductionMaximum) this.reduction = this.reductionMaximum;
            }
        }
        private void IncreaseAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.reduction = Math.Min(this.reduction + 5, 100);
        }
        private void DecreaseAppBarButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.reduction = Math.Max(this.reduction - 5, 1);
        }
        private void ChangeColorToNextButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.resizedImages.Any()) return;
            var nextBrush = new SolidColorBrush(CycleColorCollection.Next(this.resizedImages[0].borderBrush.Color));
            foreach (var resizedImage in this.resizedImages)
            {
                resizedImage.borderBrush = nextBrush;
            }
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.borderBrush)));
        }
        private void ChangeColorToPreviousButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!this.resizedImages.Any()) return;
            var nextBrush = new SolidColorBrush(CycleColorCollection.Previous(this.resizedImages[0].borderBrush.Color));
            foreach (var resizedImage in this.resizedImages)
            {
                resizedImage.borderBrush = nextBrush;
            }
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.borderBrush)));
        }
        private void NextButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this.isNextButtonEnabled) this.imageSelectFlipView.SelectedIndex++;
        }
        private void PreviousButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (this.isPreviousButtonEnabled) this.imageSelectFlipView.SelectedIndex--;
        }
        private async void AddButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Dictionary<string, WriteableBitmap> parameterToMainPage = new Dictionary<string, WriteableBitmap>();
            foreach (var resizedImage in this.resizedImages)
            {
                if (resizedImage == null) continue;
                var writeableBitmap = await resizedImage.GetResizedBitmap();
                if (writeableBitmap == null) continue;
                parameterToMainPage.Add(resizedImage.ImageName, writeableBitmap);
            }
            this.Frame.Navigate(typeof(MainPage), parameterToMainPage);
            this.ClearResizedImages();
        }
        private void CancelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage), new Dictionary<string, WriteableBitmap>());
            this.ClearResizedImages();
        }
    }
}
