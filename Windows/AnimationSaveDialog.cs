using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace AnimationExtractor
{
    public partial class AnimationSaveDialog : Window
    {
        private int framecount;
        public int AnimationWidth;
        public int AnimationHeight;

        public AnimationSaveDialog(int nframes)
        {
            InitializeComponent();
            framecount = nframes;
            AnimationWidth = (int)Math.Sqrt(nframes);
            AnimationHeight = nframes / AnimationWidth;
            saveButton.Content = "Save " + framecount.ToString() + " Frames...";
            textWidth.Text = AnimationWidth.ToString();
        }

        private bool ChangeDimension(string text, ref int textDim, ref int otherDim)
        {
            int newDim;
            bool changedDim = int.TryParse(text, out newDim) && newDim > 0 && newDim <= framecount;
            if (changedDim)
            {
                textDim = newDim;
                otherDim = framecount / newDim;
                if (newDim * otherDim != framecount)
                {
                    otherDim += 1;
                }
            }
            return changedDim;
        }

        private void Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox box = sender as TextBox;
            if (ChangeDimension(box.Text, ref AnimationWidth, ref AnimationHeight))
            {
                widthDisplay.Text = AnimationWidth.ToString();
                heightDisplay.Text = AnimationHeight.ToString();
            }
        }

        private void Dialog_Finish(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
