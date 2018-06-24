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

// ユーザー コントロールの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234236 を参照してください

namespace Diarix.UserControls
{
    public sealed partial class ImageFlipView : UserControl, INotifyPropertyChanged
    {
        private class ImageFlipViewItem
        {
            public string Name;
            public string Explanation;
            public ImageSource Image;
            public ImageFlipViewItem(string name,string explanation, ImageSource image)
            {
                this.Name = name;
                this.Explanation = explanation;
                this.Image = image;
            }
        }
        private Visibility _imageNameVisibility;
        private List<ImageFlipViewItem> items;
        private Visibility ImageNameVisibility
        {
            get => this._imageNameVisibility;
            set
            {
                this._imageNameVisibility = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.ImageNameVisibility)));
            }
        }
        private string SelectedImageName
        {
            get
            {
                if (!this.items.Any() || this.flipView.SelectedIndex == -1) return null;
                else return $"{this.flipView.SelectedIndex + 1}/{this.items.Count} {this.items[this.flipView.SelectedIndex].Name}";
            }
        }
        public string SelectedImageExplanation
        {
            get
            {
                if (!this.items.Any() || this.flipView.SelectedIndex == -1) return null;
                else return this.items[this.flipView.SelectedIndex].Explanation;
            }
            set
            {
                if (!this.items.Any() || this.flipView.SelectedIndex == -1) return;
                this.items[this.flipView.SelectedIndex].Explanation = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectedImageExplanation)));
                this.SelectedImageExplanationChanged?.Invoke(this, value);
            }
        }
        public IEnumerable<string> ImageNames => this.items.Select(item => item.Name);
        public IEnumerable<string> ImageExplanations => this.items.Select(item => item.Explanation);
        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<string> SelectedImageExplanationChanged;
        public ImageFlipView()
        {
            this.InitializeComponent();
            this.items = new List<ImageFlipViewItem>();
            this.ImageNameVisibility = Visibility.Collapsed;
        }
        public void AddImage(string imageName,string explanation, ImageSource imageSource)
        {
            this.items.Add(new ImageFlipViewItem(imageName,explanation, imageSource));
            Image image = new Image()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Uniform,
                Source = imageSource,
            };
            this.flipView.Items.Add(image);
            this.flipView.SelectedIndex = this.items.Count - 1;
        }
        public string RemoveSelectedImage()
        {
            if (!this.items.Any()) return null;
            var name = this.items[this.flipView.SelectedIndex].Name;
            this.items.RemoveAt(this.flipView.SelectedIndex);
            this.flipView.Items.RemoveAt(this.flipView.SelectedIndex);
            return name;
        }
        public IEnumerable<string> RemoveAllImages()
        {
            while (this.items.Any()) yield return this.RemoveSelectedImage();
        }
        private void UserControl_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            this.ImageNameVisibility = Visibility.Visible;
        }
        private void UserControl_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            this.ImageNameVisibility = Visibility.Collapsed;
        }
        private void flipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectedImageName)));
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.SelectedImageExplanation)));
            this.SelectedImageExplanationChanged?.Invoke(this, this.SelectedImageExplanation);
        }
    }
}
