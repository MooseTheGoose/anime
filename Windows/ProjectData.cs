using System;
using System.Windows;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;


namespace AnimationExtractor
{
    public class ProjectData
    {
        public string ImagePath;
        public int FrameWidth;
        public int FrameHeight;
        public int AnimationTimeMs;
        public List<FrameData> Frames;

        public const int MsToTicks = 10000;
        public const int MaxMsTime = int.MaxValue / MsToTicks;

        public Brush SelectedRectFillBrush;
        public Brush DeselectedRectFillBrush;
        public Brush SelectedRectStrokeBrush;
        public Brush DeselectedRectStrokeBrush;
        public Brush SelectedSkipRectFillBrush;
        public Brush DeselectedSkipRectFillBrush;
        public Brush SelectedSkipRectStrokeBrush;
        public Brush DeselectedSkipRectStrokeBrush;
        public Brush SelectedInfoBrush;
        public Brush DeselectedInfoBrush;
        public Brush BoxSelectFill;
        public Brush BoxSelectStroke;
        public double RectOpacity;

        public void InitializeDefaults()
        {
            ImagePath = null;
            FrameWidth = 64;
            FrameHeight = 64;
            AnimationTimeMs = 100;

            SelectedRectFillBrush = Brushes.Red;
            SelectedRectStrokeBrush = Brushes.OrangeRed;
            DeselectedRectFillBrush = Brushes.MediumBlue;
            DeselectedRectStrokeBrush = Brushes.Blue;
            SelectedSkipRectFillBrush = Brushes.Green;
            SelectedSkipRectStrokeBrush = Brushes.DarkGreen;
            DeselectedSkipRectFillBrush = Brushes.Violet;
            DeselectedSkipRectStrokeBrush = Brushes.BlueViolet;
            SelectedInfoBrush = Brushes.LightGray;
            DeselectedInfoBrush = Brushes.White;
            BoxSelectFill = Brushes.Yellow;
            BoxSelectStroke = Brushes.YellowGreen;
            RectOpacity = 0.5;

            Frames = new List<FrameData>();
        }

        public XDocument SaveProjectDataXML()
        {
            return new XDocument(
                new XElement("AnimeProject",
                  new XAttribute("Path", ImagePath),
                  new XAttribute("Height", FrameHeight.ToString()),
                  new XAttribute("Width", FrameWidth.ToString()),
                  new XAttribute("Ms", AnimationTimeMs.ToString()),
                  new XElement("FrameCollection", Frames.Select(f => {
                      return new XElement(
                          "Frame", 
                          new XAttribute("X", f.X.ToString()),
                          new XAttribute("Y", f.Y.ToString()),
                          new XAttribute("Skip", f.Skip.ToString()),
                          new XAttribute("Selected", f.Selected.ToString()));
                  }))));
        }

        public void SaveProjectData(string fname)
        {
            using (Stream s = File.Create(fname))
            {
                SaveProjectDataXML().Save(s);
            }
        }

        public void LoadProjectDataXML(XDocument doc)
        {
            InitializeDefaults();
            XElement root = doc.Root;
            if (root != null)
            {
                XAttribute pathattr = root.Attribute("Path");
                XAttribute heightattr = root.Attribute("Height");
                XAttribute widthattr = root.Attribute("Width");
                XAttribute msattr = root.Attribute("Ms");
                XElement framecoll = root.Element("FrameCollection");
                int val = 0;
                bool truth = false;
                if (pathattr != null)
                {
                    ImagePath = pathattr.Value;
                }
                if (heightattr != null && int.TryParse(heightattr.Value, out val))
                {
                    FrameHeight = val;
                }
                if (widthattr != null && int.TryParse(widthattr.Value, out val))
                {
                    FrameWidth = val;
                }
                if (msattr != null && int.TryParse(msattr.Value, out val))
                {
                    AnimationTimeMs = val;
                }

                if (framecoll != null)
                {
                    foreach (XElement frame in framecoll.Elements())
                    {
                        XAttribute x = frame.Attribute("X");
                        XAttribute y = frame.Attribute("Y");
                        XAttribute selected = frame.Attribute("Selected");
                        XAttribute skipped = frame.Attribute("Skip");
                        FrameData fd = AddFrame();
                        int xval = fd.X;
                        int yval = fd.Y;

                        if (x != null && int.TryParse(x.Value, out val))
                        {
                            xval = val;
                        }
                        if (y != null && int.TryParse(y.Value, out val))
                        {
                            yval = val;
                        }
                        fd.SetNewCoords(xval, yval);

                        if (selected != null && bool.TryParse(selected.Value, out truth))
                        {
                            SelectFrame(fd, truth); 
                        }
                        if (skipped != null && bool.TryParse(skipped.Value, out truth))
                        {
                            SkipFrame(fd, truth);
                        }
                    }
                }
            }
        }

        public void LoadProjectData(string fname)
        {
            using (Stream s = File.OpenRead(fname))
            {
                LoadProjectDataXML(XDocument.Load(fname));
            }
        }

        public ProjectData()
        {
            InitializeDefaults();
        }

        public void RecolorFrame(FrameData data)
        {
            if (data.Selected)
            {
                data.InfoPanel.Background = SelectedInfoBrush;
                if (data.Skip)
                {
                    data.WinRect.Stroke = SelectedSkipRectStrokeBrush;
                    data.WinRect.Fill = SelectedSkipRectFillBrush;
                }
                else 
                {
                    data.WinRect.Stroke = SelectedRectStrokeBrush;
                    data.WinRect.Fill = SelectedRectFillBrush;
                }
            }
            else
            {
                data.InfoPanel.Background = DeselectedInfoBrush;
                if (data.Skip)
                {
                    data.WinRect.Stroke = DeselectedSkipRectStrokeBrush;
                    data.WinRect.Fill = DeselectedSkipRectFillBrush;
                }
                else
                {
                    data.WinRect.Stroke = DeselectedRectStrokeBrush;
                    data.WinRect.Fill = DeselectedRectFillBrush;
                }
            }
        }

        public void SelectFrame(FrameData data)
        {
            data.Selected = true;
            RecolorFrame(data);
        }

        public void DeselectFrame(FrameData data)
        {
            data.Selected = false;
            RecolorFrame(data);
        }
        public void SelectFrame(FrameData data, bool truth)
        {
            if (truth)
            {
                SelectFrame(data);
            }
            else
            {
                DeselectFrame(data);
            }
        }

        public void SkipFrame(FrameData data)
        {
            data.Skip = true;
            data.SkipButton.Content = "Unskip";
            RecolorFrame(data);
        }
        public void DisableSkipFrame(FrameData data)
        {
            data.Skip = false;
            data.SkipButton.Content = "Skip";
            RecolorFrame(data);
        }

        public void SkipFrame(FrameData data, bool truth)
        {
            if (truth)
            {
                SkipFrame(data);
            }
            else
            {
                DisableSkipFrame(data);
            }
        }

        public FrameData AddFrame()
        {
            FrameData data = new FrameData();
            data.WinRect.Opacity = RectOpacity;
            SelectFrame(data);
            DisableSkipFrame(data);
            Frames.Add(data);
            return data;
        }

        public BitmapSource RenderFrameDataToBitmap(BitmapImage refBitmap, FrameData fd)
        {
            int x = Math.Clamp(fd.X, 0, refBitmap.PixelWidth);
            int y = Math.Clamp(fd.Y, 0, refBitmap.PixelHeight);
            int right = (int)Math.Clamp((long)(fd.X + FrameWidth), (long)x, (long)refBitmap.PixelWidth);
            int bottom = (int)Math.Clamp((long)(fd.Y + FrameHeight), (long)y, (long)refBitmap.PixelHeight);

            RenderTargetBitmap bmp = new RenderTargetBitmap(FrameWidth, FrameHeight, 96, 96, PixelFormats.Pbgra32);

            if(right != x && bottom != y)
            {
                CroppedBitmap cbmp = new CroppedBitmap(refBitmap, new Int32Rect(x, y, right-x, bottom-y));
                DrawingVisual vis = new DrawingVisual();
                using(DrawingContext ctx = vis.RenderOpen())
                {
                    ctx.DrawImage(cbmp, new Rect(0, 0, FrameWidth, FrameHeight));
                }
                bmp.Render(vis);
            }
            return bmp;
        }
    }
}