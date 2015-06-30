using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HalconDotNet;
using System.Windows;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.ComponentModel;
using System.IO;

namespace PCBInspection.Model
{
    class ImageProcessor : IDisposable
    {
        private HWindow windowHandle;

        // Local iconic variables 
        
        HObject ho_Image, ho_Bright, ho_RectPCB, ho_ImagePCB;
        HObject ho_Regions, ho_RectRegions, ho_ConnectedRegions, ho_FillUpRegions;
        HObject ho_SelectedRegions, ho_modelFileRegions;
        HObject ho_region = null, ho_modelRegion = null;

        HTuple hv_area, hv_modelArea;
        HTuple hv_y, hv_x, hv_modelY, hv_modelX;
        HObject ho_SelectedModelRegions;

        // Local control variables 
        HTuple hv_RegionCount, hv_FileRegionCount;
        HTuple hv_width, hv_height;

        public ImageProcessor(HWindow windowHandle)
        {
            this.windowHandle = windowHandle;
            InitVariables();
        }

        //加载图片
        #region
        private string imagePath = "Image";
        public string ImagePath
        {
            get { return imagePath; }
            set { imagePath = value; }
        }

        public bool LoadImage(string imagePath)
        {
            windowHandle.ClearWindow();
            HObject ho_ImageRGB;
            HOperatorSet.ReadImage(out ho_ImageRGB, imagePath);
            HOperatorSet.GetImageSize(ho_ImageRGB, out hv_width, out hv_height);
            windowHandle.SetPart(0, 0, hv_height - 1, hv_width - 1);

            HTuple channelCount;
            HOperatorSet.CountChannels(ho_ImageRGB, out channelCount);

            ho_Image.Dispose();
            if (channelCount >= 3)
            {
                HOperatorSet.AccessChannel(ho_ImageRGB, out ho_Image, 2);
            }
            else
            {
                HOperatorSet.AccessChannel(ho_ImageRGB, out ho_Image, 1);
            }
            windowHandle.DispObj(ho_Image);
            return true;
        }

        public bool LoadImage()
        {
            string filePath = this.ImagePath;
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(FilePathUtility.GetAssemblyPath(), filePath);
            }

            return this.LoadImage(filePath);
        }
        #endregion

        //加载模板图片
        #region
        private string modelImagePath = "Model";
        public string ModelImagePath
        {
            get { return modelImagePath; }
            set { modelImagePath = value; }
        }

        public bool LoadModelImage()
        {
            string filePath = this.ModelImagePath;
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(FilePathUtility.GetAssemblyPath(), filePath);
            }
            return this.LoadImage(filePath);
        }

        public void ResetModelImageFile()
        {
            this.ModelImagePath = "Model";
        }
        #endregion

        //找目标芯片
        //1 - SelectROI
        #region
        private int minBackColorValue = 100;
        public int MinBackColorValue
        {
            get { return minBackColorValue; }
            set { minBackColorValue = value; }
        }

        private int maxBackColorValue = 255;
        public int MaxBackColorValue
        {
            get { return maxBackColorValue; }
            set { maxBackColorValue = value; }
        }

        public bool SelectROI()
        {
            ho_Bright.Dispose();
            HOperatorSet.Threshold(ho_Image, out ho_Bright, this.MinBackColorValue, this.MaxBackColorValue);
            ho_RectPCB.Dispose();
            HOperatorSet.ShapeTrans(ho_Bright, out ho_RectPCB, "rectangle2");
            if (windowHandle.IsInitialized())
            {
                windowHandle.DispObj(ho_Image);
                windowHandle.SetColor("green");
                windowHandle.SetLineWidth(3);
                windowHandle.SetDraw("margin");
                windowHandle.DispObj(ho_RectPCB);
            }
            return true;
        }
        #endregion

        //2 - FindTartgetIC
        #region
        private int minICColorValue = 66;
        public int MinICColorValue
        {
            get { return minICColorValue; }
            set { minICColorValue = value; }
        }

        private int maxICColorValue = 98;
        public int MaxICColorValue
        {
            get { return maxICColorValue; }
            set { maxICColorValue = value; }
        }

        public bool FindTargetICs()
        {
            ho_ImagePCB.Dispose();
            HOperatorSet.ReduceDomain(ho_Image, ho_RectPCB, out ho_ImagePCB);

            ho_Regions.Dispose();
            HOperatorSet.Threshold(ho_ImagePCB, out ho_Regions, this.MinICColorValue, this.MaxICColorValue);

            windowHandle.SetColor("blue");
            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("fill");
            windowHandle.DispObj(ho_Regions);

            return true;
        }
        #endregion

        //3 - SelectTargetICs
        #region
        private double rectSimilarityValue = 90;
        public double RectSimilarityValue
        {
            get { return rectSimilarityValue; }
            set { rectSimilarityValue = value; }
        }

        private double minArea = 6000;
        public double MinArea
        {
            get { return minArea; }
            set { minArea = value; }
        }

        private double maxArea = 20000;
        public double MaxArea
        {
            get { return maxArea; }
            set { maxArea = value; }
        }
        

        public bool SelectTargetICs()
        {
            //ho_RectRegions.Dispose();
            //HOperatorSet.OpeningRectangle1(ho_Regions, out ho_RectRegions, 10, 10);  //TODO： 改成膨胀腐蚀

            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_Regions, out ho_ConnectedRegions);

            ho_FillUpRegions.Dispose();
            HOperatorSet.FillUp(ho_ConnectedRegions, out ho_FillUpRegions);

            ho_SelectedRegions.Dispose();
            HOperatorSet.SelectShape(ho_FillUpRegions, out ho_SelectedRegions, "area", "and", MinArea, MaxArea);

            //ho_SelectedRegions.Dispose();
            //HOperatorSet.SelectShapeStd(ho_ConnectedRegions, out ho_SelectedRegions, "rectangle1", this.RectSimilarityValue);
            
            HOperatorSet.CountObj(ho_SelectedRegions, out hv_RegionCount);
            if (hv_RegionCount <= 0)
            {
                return false;
            }

            if (windowHandle.IsInitialized())
            {
                windowHandle.DispObj(ho_Image);
                windowHandle.SetColor("green");
                windowHandle.SetLineWidth(3);
                windowHandle.SetDraw("margin");
                windowHandle.DispObj(ho_SelectedRegions);
            }
            return true;
        }

        private string modelFilePath = "Model.reg";
        public string ModelFilePath
        {
            get
            {
                return Path.Combine(FilePathUtility.GetAssemblyPath(), modelFilePath); 
            }
        }
        #endregion

        //保存芯片模板存文件
        public bool MakeModelFile()
        {
            HOperatorSet.CountObj(ho_SelectedRegions, out hv_RegionCount);
            if (hv_RegionCount <= 0)
            {
                return false;
            }
            HOperatorSet.WriteRegion(ho_SelectedRegions, this.ModelFilePath);
            return true;
        }


        //读取芯片模板文件
        public bool LoadModelFile()
        {
            ho_modelFileRegions.Dispose();
            HOperatorSet.ReadRegion(out ho_modelFileRegions, this.ModelFilePath);
            HOperatorSet.CountObj(ho_modelFileRegions, out hv_FileRegionCount);
            return (hv_FileRegionCount > 0);
        }

        //判断是否为合格芯片
        public bool CompareICRegions()
        {
            if (windowHandle.IsInitialized())
            {
                windowHandle.DispObj(ho_Image);
            }

            List<RegionData> modelRegionDatas = new List<RegionData>();

            int modelCount = (int)hv_FileRegionCount.I;
            for (int index = 1; index <= modelCount; index++)
            {
                ho_modelRegion.Dispose();
                HOperatorSet.SelectObj(ho_modelFileRegions, out ho_modelRegion, index);
                HOperatorSet.AreaCenter(ho_modelRegion, out hv_modelArea, out hv_modelY, out hv_modelX);
                modelRegionDatas.Add(new RegionData() { X = hv_modelX.D, Y = hv_modelY.D, Area = hv_modelArea.D });
                if (windowHandle.IsInitialized())
                {
                    windowHandle.SetColor("green");
                    //windowHandle.SetTposition((int)(hv_modelY.D - 2), (int)(hv_modelX.D - 2));
                    //windowHandle.WriteString(string.Format("{0}", index));
                    windowHandle.DispObj(ho_modelRegion);
                }
            }

            List<RegionData> regionDatas = new List<RegionData>();
            var count = hv_RegionCount.I;
            for (int index = 1; index <= count; index++)
            {
                ho_region.Dispose();
                HOperatorSet.SelectObj(ho_SelectedRegions, out ho_region, index);
                HOperatorSet.AreaCenter(ho_region, out hv_area, out hv_y, out hv_x);
                regionDatas.Add(new RegionData() { X = hv_x.D, Y = hv_y.D, Area = hv_area.D });
                if (windowHandle.IsInitialized())
                {
                    windowHandle.SetColor("red");
                    //windowHandle.SetTposition((int)(hv_y.D + 2), (int)(hv_x.D + 2));
                    //windowHandle.WriteString(string.Format("{0}", index));
                    windowHandle.DispObj(ho_region);
                }
            }

            bool flag = false;
            if (count == modelCount)
            {
                for (int i = 0; i < count; i++)
                {
                    var modelReg = modelRegionDatas[i];
                    var reg = regionDatas[i];
                    if (!(Math.Abs(modelReg.X - reg.X) < 5.0 && Math.Abs(modelReg.Y - reg.Y) < 5.0 && Math.Abs(modelReg.Area - reg.Area) < 20))
                    {
                        flag = false;
                        break;
                    }
                    else
                    {
                        flag = true;
                    }
                }
            }

            set_display_font(windowHandle, 50, "mono", "true", "false");
            windowHandle.SetColor("red");
            windowHandle.SetTposition((int)20, (int)40);
            windowHandle.WriteString(flag ? "OK" : "NG");
            return flag;
        }

        public void InitVariables()
        {
            HOperatorSet.GenEmptyObj(out ho_Image);
            HOperatorSet.GenEmptyObj(out ho_Bright);
            HOperatorSet.GenEmptyObj(out ho_RectPCB);
            HOperatorSet.GenEmptyObj(out ho_ImagePCB);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_RectRegions);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_FillUpRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);
            HOperatorSet.GenEmptyObj(out ho_SelectedModelRegions);
            HOperatorSet.GenEmptyObj(out ho_region);
            HOperatorSet.GenEmptyObj(out ho_modelRegion);
            HOperatorSet.GenEmptyObj(out ho_modelFileRegions);
        }

        public void UninitVariables()
        {
            ho_Image.Dispose();
            ho_Bright.Dispose();
            ho_RectPCB.Dispose();
            ho_ImagePCB.Dispose();
            ho_Regions.Dispose();
            ho_RectRegions.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_FillUpRegions.Dispose();
            ho_SelectedModelRegions.Dispose();
            ho_SelectedRegions.Dispose();
            ho_region.Dispose();
            ho_modelRegion.Dispose();
            ho_modelFileRegions.Dispose();
        }

        public void Dispose()
        {
            UninitVariables();
        }

        public void set_display_font(HTuple hv_WindowHandle, HTuple hv_Size, HTuple hv_Font, HTuple hv_Bold, HTuple hv_Slant)
        {
            // Local control variables 

            HTuple hv_OS, hv_Exception = new HTuple();
            HTuple hv_AllowedFontSizes = new HTuple(), hv_Distances = new HTuple();
            HTuple hv_Indices = new HTuple();

            HTuple hv_Bold_COPY_INP_TMP = hv_Bold.Clone();
            HTuple hv_Font_COPY_INP_TMP = hv_Font.Clone();
            HTuple hv_Size_COPY_INP_TMP = hv_Size.Clone();
            HTuple hv_Slant_COPY_INP_TMP = hv_Slant.Clone();

            // Initialize local and output iconic variables 

            //This procedure sets the text font of the current window with
            //the specified attributes.
            //It is assumed that following fonts are installed on the system:
            //Windows: Courier New, Arial Times New Roman
            //Linux: courier, helvetica, times
            //Because fonts are displayed smaller on Linux than on Windows,
            //a scaling factor of 1.25 is used the get comparable results.
            //For Linux, only a limited number of font sizes is supported,
            //to get comparable results, it is recommended to use one of the
            //following sizes: 9, 11, 14, 16, 20, 27
            //(which will be mapped internally on Linux systems to 11, 14, 17, 20, 25, 34)
            //
            //input parameters:
            //WindowHandle: The graphics window for which the font will be set
            //Size: The font size. If Size=-1, the default of 16 is used.
            //Bold: If set to 'true', a bold font is used
            //Slant: If set to 'true', a slanted font is used
            //
            HOperatorSet.GetSystem("operating_system", out hv_OS);
            if ((int)((new HTuple(hv_Size_COPY_INP_TMP.TupleEqual(new HTuple()))).TupleOr(
                new HTuple(hv_Size_COPY_INP_TMP.TupleEqual(-1)))) != 0)
            {
                hv_Size_COPY_INP_TMP = 16;
            }
            if ((int)(new HTuple((((hv_OS.TupleStrFirstN(2)).TupleStrLastN(0))).TupleEqual(
                "Win"))) != 0)
            {
                //set font on Windows systems
                if ((int)((new HTuple((new HTuple(hv_Font_COPY_INP_TMP.TupleEqual("mono"))).TupleOr(
                    new HTuple(hv_Font_COPY_INP_TMP.TupleEqual("Courier"))))).TupleOr(new HTuple(hv_Font_COPY_INP_TMP.TupleEqual(
                    "courier")))) != 0)
                {
                    hv_Font_COPY_INP_TMP = "Courier New";
                }
                else if ((int)(new HTuple(hv_Font_COPY_INP_TMP.TupleEqual("sans"))) != 0)
                {
                    hv_Font_COPY_INP_TMP = "Arial";
                }
                else if ((int)(new HTuple(hv_Font_COPY_INP_TMP.TupleEqual("serif"))) != 0)
                {
                    hv_Font_COPY_INP_TMP = "Times New Roman";
                }
                if ((int)(new HTuple(hv_Bold_COPY_INP_TMP.TupleEqual("true"))) != 0)
                {
                    hv_Bold_COPY_INP_TMP = 1;
                }
                else if ((int)(new HTuple(hv_Bold_COPY_INP_TMP.TupleEqual("false"))) != 0)
                {
                    hv_Bold_COPY_INP_TMP = 0;
                }
                else
                {
                    hv_Exception = "Wrong value of control parameter Bold";
                    throw new HalconException(hv_Exception);
                }
                if ((int)(new HTuple(hv_Slant_COPY_INP_TMP.TupleEqual("true"))) != 0)
                {
                    hv_Slant_COPY_INP_TMP = 1;
                }
                else if ((int)(new HTuple(hv_Slant_COPY_INP_TMP.TupleEqual("false"))) != 0)
                {
                    hv_Slant_COPY_INP_TMP = 0;
                }
                else
                {
                    hv_Exception = "Wrong value of control parameter Slant";
                    throw new HalconException(hv_Exception);
                }
                try
                {
                    HOperatorSet.SetFont(hv_WindowHandle, ((((((("-" + hv_Font_COPY_INP_TMP) + "-") + hv_Size_COPY_INP_TMP) + "-*-") + hv_Slant_COPY_INP_TMP) + "-*-*-") + hv_Bold_COPY_INP_TMP) + "-");
                }
                // catch (Exception) 
                catch (HalconException HDevExpDefaultException1)
                {
                    HDevExpDefaultException1.ToHTuple(out hv_Exception);
                    throw new HalconException(hv_Exception);
                }
            }
            else
            {
                //set font for UNIX systems
                hv_Size_COPY_INP_TMP = hv_Size_COPY_INP_TMP * 1.25;
                hv_AllowedFontSizes = new HTuple();
                hv_AllowedFontSizes[0] = 11;
                hv_AllowedFontSizes[1] = 14;
                hv_AllowedFontSizes[2] = 17;
                hv_AllowedFontSizes[3] = 20;
                hv_AllowedFontSizes[4] = 25;
                hv_AllowedFontSizes[5] = 34;
                if ((int)(new HTuple(((hv_AllowedFontSizes.TupleFind(hv_Size_COPY_INP_TMP))).TupleEqual(
                    -1))) != 0)
                {
                    hv_Distances = ((hv_AllowedFontSizes - hv_Size_COPY_INP_TMP)).TupleAbs();
                    HOperatorSet.TupleSortIndex(hv_Distances, out hv_Indices);
                    hv_Size_COPY_INP_TMP = hv_AllowedFontSizes.TupleSelect(hv_Indices.TupleSelect(
                        0));
                }
                if ((int)((new HTuple(hv_Font_COPY_INP_TMP.TupleEqual("mono"))).TupleOr(new HTuple(hv_Font_COPY_INP_TMP.TupleEqual(
                    "Courier")))) != 0)
                {
                    hv_Font_COPY_INP_TMP = "courier";
                }
                else if ((int)(new HTuple(hv_Font_COPY_INP_TMP.TupleEqual("sans"))) != 0)
                {
                    hv_Font_COPY_INP_TMP = "helvetica";
                }
                else if ((int)(new HTuple(hv_Font_COPY_INP_TMP.TupleEqual("serif"))) != 0)
                {
                    hv_Font_COPY_INP_TMP = "times";
                }
                if ((int)(new HTuple(hv_Bold_COPY_INP_TMP.TupleEqual("true"))) != 0)
                {
                    hv_Bold_COPY_INP_TMP = "bold";
                }
                else if ((int)(new HTuple(hv_Bold_COPY_INP_TMP.TupleEqual("false"))) != 0)
                {
                    hv_Bold_COPY_INP_TMP = "medium";
                }
                else
                {
                    hv_Exception = "Wrong value of control parameter Bold";
                    throw new HalconException(hv_Exception);
                }
                if ((int)(new HTuple(hv_Slant_COPY_INP_TMP.TupleEqual("true"))) != 0)
                {
                    if ((int)(new HTuple(hv_Font_COPY_INP_TMP.TupleEqual("times"))) != 0)
                    {
                        hv_Slant_COPY_INP_TMP = "i";
                    }
                    else
                    {
                        hv_Slant_COPY_INP_TMP = "o";
                    }
                }
                else if ((int)(new HTuple(hv_Slant_COPY_INP_TMP.TupleEqual("false"))) != 0)
                {
                    hv_Slant_COPY_INP_TMP = "r";
                }
                else
                {
                    hv_Exception = "Wrong value of control parameter Slant";
                    throw new HalconException(hv_Exception);
                }
                try
                {
                    HOperatorSet.SetFont(hv_WindowHandle, ((((((("-adobe-" + hv_Font_COPY_INP_TMP) + "-") + hv_Bold_COPY_INP_TMP) + "-") + hv_Slant_COPY_INP_TMP) + "-normal-*-") + hv_Size_COPY_INP_TMP) + "-*-*-*-*-*-*-*");
                }
                // catch (Exception) 
                catch (HalconException HDevExpDefaultException1)
                {
                    HDevExpDefaultException1.ToHTuple(out hv_Exception);
                    throw new HalconException(hv_Exception);
                }
            }
            return;
        }

        #region Draw ROI Rectangle1

        private ObservableCollection<ICModelParameter> modelICs = new ObservableCollection<ICModelParameter>();
        public ObservableCollection<ICModelParameter> ModelICs
        {
            get { return modelICs; }
        }

        private int countOfICs = 0;
        public int CountOfICs
        {
            get { return countOfICs; }
            set { countOfICs = value; }
        }

        public ICModelParameter DrawRectangle1()
        {
            double hv_Row1, hv_Column1, hv_Row2, hv_Column2;
            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("margin");
            windowHandle.SetColor("red");
            windowHandle.DrawRectangle1(out hv_Row1, out hv_Column1, out hv_Row2, out hv_Column2);

            HObject ho_Rectangle;
            HOperatorSet.GenRectangle1(out ho_Rectangle, hv_Row1, hv_Column1, hv_Row2, hv_Column2);

            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("margin");
            windowHandle.SetColor("blue");
            windowHandle.DispObj(ho_Rectangle);

            
            var icmodel = new ICModelParameter() { Row1 = hv_Row1, Column1 = hv_Column1, Row2 = hv_Row2, Column2 = hv_Column2,
            Label = string.Format("IC_{0}", CountOfICs++),
            };

            this.ModelICs.Add(icmodel);
            return icmodel;
        }

        public bool ThresholdOneIC(ICModelParameter icModel)
        {
            windowHandle.ClearWindow();
            windowHandle.DispObj(ho_Image);

            HObject ho_Rectangle, ho_ImageRegion;
            HObject ho_Regions, ho_ConnectedRegions, ho_RegionFillUp;
            HObject ho_SelectedRegions;

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_ImageRegion);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);

            ho_Rectangle.Dispose();
            HOperatorSet.GenRectangle1(out ho_Rectangle, icModel.Row1, icModel.Column1, icModel.Row2, icModel.Column2);
            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("margin");
            windowHandle.SetColor("blue");
            windowHandle.DispObj(ho_Rectangle);

            ho_ImageRegion.Dispose();
            HOperatorSet.ReduceDomain(ho_Image, ho_Rectangle, out ho_ImageRegion);

            ho_Regions.Dispose();
            HOperatorSet.Threshold(ho_ImageRegion, out ho_Regions, icModel.MinGray, icModel.MaxGray);

            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("fill");
            windowHandle.SetColor("red");
            windowHandle.DispObj(ho_Regions);

            ho_Rectangle.Dispose();
            ho_ImageRegion.Dispose();
            ho_Regions.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_SelectedRegions.Dispose();

            return true;
        }

        public bool ProcessOneIC(ICModelParameter icModel)
        {
            windowHandle.ClearWindow();
            windowHandle.DispObj(ho_Image);

            HObject ho_Rectangle, ho_ImageRegion;
            HObject ho_Regions, ho_ConnectedRegions, ho_RegionFillUp;
            HObject ho_SelectedRegions;

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_ImageRegion);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);

            ho_Rectangle.Dispose();
            HOperatorSet.GenRectangle1(out ho_Rectangle, icModel.Row1, icModel.Column1, icModel.Row2, icModel.Column2);
            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("margin");
            windowHandle.SetColor("blue");
            windowHandle.DispObj(ho_Rectangle);

            ho_ImageRegion.Dispose();
            HOperatorSet.ReduceDomain(ho_Image, ho_Rectangle, out ho_ImageRegion);
           
            ho_Regions.Dispose();
            HOperatorSet.Threshold(ho_ImageRegion, out ho_Regions, icModel.MinGray, icModel.MaxGray);


            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_Regions, out ho_ConnectedRegions);

            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_ConnectedRegions, out ho_RegionFillUp);

            ho_SelectedRegions.Dispose();
            HOperatorSet.SelectShape(ho_RegionFillUp, out ho_SelectedRegions, "area", "and", icModel.MinArea, icModel.MaxArea);

            HTuple regionCount;
            HOperatorSet.CountObj(ho_SelectedRegions, out regionCount);


            if (regionCount.I == 1)
            {
                HTuple icArea;
                HTuple icCentRow;
                HTuple icCentCol;
                HOperatorSet.AreaCenter(ho_SelectedRegions, out icArea, out icCentRow, out icCentCol);

                icModel.AreaIC = icArea.D;
                icModel.CenterRowIC = icCentRow.D;
                icModel.CenterColIC = icCentCol.D;
                icModel.InvalidIC = true;

            }
            else
            {
                icModel.InvalidIC = false;
            }

            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("fill");
            windowHandle.SetColor("red");
            windowHandle.DispObj(ho_SelectedRegions);

            ho_Rectangle.Dispose();
            ho_ImageRegion.Dispose();
            ho_Regions.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_SelectedRegions.Dispose();

            return true;
        }

        #endregion

        public bool ProcessConnectionROI(ICModelParameter icModel, out string regionInfo)
        {
            regionInfo = string.Empty;

            windowHandle.ClearWindow();
            windowHandle.DispObj(ho_Image);

            HObject ho_Rectangle, ho_ImageRegion;
            HObject ho_Regions, ho_ConnectedRegions, ho_RegionFillUp;
            HObject ho_SelectedRegions;

            // Initialize local and output iconic variables 
            HOperatorSet.GenEmptyObj(out ho_Rectangle);
            HOperatorSet.GenEmptyObj(out ho_ImageRegion);
            HOperatorSet.GenEmptyObj(out ho_Regions);
            HOperatorSet.GenEmptyObj(out ho_ConnectedRegions);
            HOperatorSet.GenEmptyObj(out ho_RegionFillUp);
            HOperatorSet.GenEmptyObj(out ho_SelectedRegions);

            ho_Rectangle.Dispose();
            HOperatorSet.GenRectangle1(out ho_Rectangle, icModel.Row1, icModel.Column1, icModel.Row2, icModel.Column2);
            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("margin");
            windowHandle.SetColor("blue");
            windowHandle.DispObj(ho_Rectangle);

            ho_ImageRegion.Dispose();
            HOperatorSet.ReduceDomain(ho_Image, ho_Rectangle, out ho_ImageRegion);

            ho_Regions.Dispose();
            HOperatorSet.Threshold(ho_ImageRegion, out ho_Regions, icModel.MinGray, icModel.MaxGray);

            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_Regions, out ho_ConnectedRegions);

            ho_RegionFillUp.Dispose();
            HOperatorSet.FillUp(ho_ConnectedRegions, out ho_RegionFillUp);

            HTuple areas, rows, cols;
            HOperatorSet.AreaCenter(ho_RegionFillUp, out areas, out rows, out cols);

            double maxAres = 0;
            int regionCount = (int)((new HTuple(areas.TupleLength())));
            for (int index = 0; index < regionCount - 1; index++)
            {
                double area = areas.TupleSelect(index);
                if (maxAres < area)
                {
                    maxAres = area;
                }
            }

            regionInfo = string.Format("最大区域面积为{0}", maxAres);
            
            windowHandle.SetLineWidth(3);
            windowHandle.SetDraw("fill");
            windowHandle.SetColor("red");
            windowHandle.DispObj(ho_RegionFillUp);

            ho_Rectangle.Dispose();
            ho_ImageRegion.Dispose();
            ho_Regions.Dispose();
            ho_ConnectedRegions.Dispose();
            ho_RegionFillUp.Dispose();
            ho_SelectedRegions.Dispose();

            return true;
        }

        private string icModelsFilePath = "ICModels.xml";
        public string ICModelsFilePath
        {
            get 
            { 
                return Path.Combine(FilePathUtility.GetAssemblyPath(), icModelsFilePath);
            }
        }

        public bool SaveICModels()
        {
            if (this.ModelICs == null || this.ModelICs.Count <= 0)
            {
                MessageBox.Show("没有可保存的模板数据", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            try
            {
                XDocument doc = new XDocument();
                var icModles = new XElement("ICModels");
                foreach (var icmodel in this.ModelICs)
                {
                    icModles.Add(icmodel.ToXElement());
                }
                doc.Add(icModles);
                doc.Save(this.ICModelsFilePath);
            }
            catch
            {
                MessageBox.Show("保存模板文件失败", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            MessageBox.Show("保存模板文件成功！", "", MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }



        public bool LoadICModels()
        {
            try
            {
                XDocument doc = XDocument.Load(this.ICModelsFilePath);
                if (doc.Root != null && doc.Root.Elements("ICModel") != null && doc.Root.Elements("ICModel").Count() > 0)
                {
                    this.ModelICs.Clear();
                    foreach (var icmodelElement in doc.Root.Elements("ICModel"))
                    {
                        var icmodel = new ICModelParameter();
                        icmodel.FromXElement(icmodelElement);
                        this.ModelICs.Add(icmodel);
                    }
                    this.CountOfICs = this.ModelICs.Count();
                }
            }
            catch
            {
                return false;
            }
            return true;
        }


        public bool Detect()
        {
            bool result = true;
            //if (this.LoadICModels())
            //{
                int icCount = this.ModelICs.Count;
                var detectedICs = new List<ICModelParameter>();
                for (int i = 0; i < icCount; i++)
                {
                    var icmodel = this.ModelICs[i];

                    var detectedIc = new ICModelParameter();
                    icmodel.CopyProcessParam(ref detectedIc);

                    this.ProcessOneIC(detectedIc);

                    detectedICs.Add(detectedIc);

                    result = this.CompareICs(detectedIc, icmodel) ? result : false;
                }

                windowHandle.ClearWindow();
                windowHandle.SetDraw("margin");
                windowHandle.DispObj(ho_Image);

                for (int i = 0; i < icCount; i++)
                {
                    var icmodel = this.ModelICs[i];

                    var detectedIc = detectedICs[i];

                    result = this.CompareICs(detectedIc, icmodel) ? result : false;
                }
            //}
            return result;
        }

        private bool CompareICs(ICModelParameter detectedIc, ICModelParameter icmodel)
        {
            if(icmodel.InvalidIC)
            {
                bool result = false;
                if (detectedIc.InvalidIC)
                {
                    if (Math.Abs(icmodel.CenterRowIC - detectedIc.CenterRowIC) < 20)
                    {
                        if (Math.Abs(icmodel.CenterColIC - detectedIc.CenterColIC) < 20)
                        {
                            if (icmodel.AreaIC * 0.7 <= detectedIc.AreaIC && icmodel.AreaIC * 1.3 >= detectedIc.AreaIC)
                            {
                                result = true;
                            }
                        }
                    }
                }
                else
                {
                    result = false;
                }

                windowHandle.SetColor(result ? "green" : "red");

                windowHandle.DispRectangle1(icmodel.Row1, icmodel.Column1, icmodel.Row2, icmodel.Column2);

                windowHandle.SetTposition((int)icmodel.CenterRowIC, (int)icmodel.CenterColIC);

                windowHandle.WriteString(result ? "OK" : "NG");

                return result;
            }
            return true;
        }

        public bool DisplayAllICModels()
        {
            if (this.ModelICs != null && this.ModelICs.Count > 0)
            {
                windowHandle.ClearWindow();
                windowHandle.SetColor("green");
                windowHandle.SetLineWidth(3);
                windowHandle.SetDraw("margin");
                windowHandle.DispObj(ho_Image);

                foreach (var icmodel in this.ModelICs)
                {
                    windowHandle.DispRectangle1(icmodel.Row1, icmodel.Column1, icmodel.Row2, icmodel.Column2);
                }

                return true;
            }
            return false;
        }
    }


    public class ICModelParameter : INotifyPropertyChanged
    {

        public bool CopyProcessParam(ref ICModelParameter detectedIC)
        {
            detectedIC.Row1 = this.Row1;
            detectedIC.Column1 = this.Column1;
            detectedIC.Row2 = this.Row2;
            detectedIC.Column2 = this.Column2;

            detectedIC.MinGray = this.MinGray;
            detectedIC.MaxGray = this.MaxGray;

            detectedIC.MinArea = this.MinArea;
            detectedIC.MaxArea = this.MaxArea;

            return true;
        }

        public XElement ToXElement()
        {
            var icXElement = new XElement("ICModel");
            icXElement.Add(new XAttribute("Label", this.Label.ToString()));
            icXElement.Add(new XAttribute("Row1", this.Row1.ToString()));
            icXElement.Add(new XAttribute("Column1", this.Column1.ToString()));
            icXElement.Add(new XAttribute("Row2", this.Row2.ToString()));
            icXElement.Add(new XAttribute("Column2", this.Column2.ToString()));

            icXElement.Add(new XAttribute("MinGray", this.MinGray.ToString()));
            icXElement.Add(new XAttribute("MaxGray", this.MaxGray.ToString()));

            icXElement.Add(new XAttribute("MinArea", this.MinArea.ToString()));
            icXElement.Add(new XAttribute("MaxArea", this.MaxArea.ToString()));

            icXElement.Add(new XAttribute("AreaIC", this.AreaIC.ToString()));

            icXElement.Add(new XAttribute("CenterRowIC", this.CenterRowIC.ToString()));
            icXElement.Add(new XAttribute("CenterColIC", this.CenterColIC.ToString()));

            return icXElement;
        }

        public bool FromXElement(XElement icXElement)
        {
            try
            {
                this.Label = icXElement.Attribute("Label").Value;

                this.Row1 = double.Parse(icXElement.Attribute("Row1").Value);
                this.Column1 = double.Parse(icXElement.Attribute("Column1").Value);
                this.Row2 = double.Parse(icXElement.Attribute("Row2").Value);
                this.Column2 = double.Parse(icXElement.Attribute("Column2").Value);

                this.MinGray = double.Parse(icXElement.Attribute("MinGray").Value);
                this.MaxGray = double.Parse(icXElement.Attribute("MaxGray").Value);

                this.MinArea = double.Parse(icXElement.Attribute("MinArea").Value);
                this.MaxArea = double.Parse(icXElement.Attribute("MaxArea").Value);

                this.AreaIC = double.Parse(icXElement.Attribute("AreaIC").Value);

                this.CenterRowIC = double.Parse(icXElement.Attribute("CenterRowIC").Value);
                this.CenterColIC = double.Parse(icXElement.Attribute("CenterColIC").Value);

            }
            catch
            {
                return false;
            }
            return true;
        }


        private string label;
        public string Label
        {
            get { return label; }
            set { label = value; OnPropertyChanged("Label"); }
        }

        //ROI Retangle
        private double hv_Row1;
        public double Row1
        {
            get { return hv_Row1; }
            set { hv_Row1 = value; OnPropertyChanged("Row1"); }
        }

        private double hv_Column1;
        public double Column1
        {
            get { return hv_Column1; }
            set { hv_Column1 = value; OnPropertyChanged("Column1"); }
        }

        private double hv_Row2;
        public double Row2
        {
            get { return hv_Row2; }
            set { hv_Row2 = value; OnPropertyChanged("Row2"); }
        }
        private double hv_Column2;
        public double Column2
        {
            get { return hv_Column2; }
            set { hv_Column2 = value; OnPropertyChanged("Column2"); }
        }

        private double minGray = 0;
        public double MinGray
        {
            get { return minGray; }
            set 
            { 
                minGray = value; 
                OnPropertyChanged("MinGray");
                System.Diagnostics.Trace.WriteLine(string.Format("MinGray = {0}", minGray));
            }
        }
 
        private double maxGray = 255;
        public double MaxGray
        {
            get { return maxGray; }
            set 
            { 
                maxGray = value; 
                OnPropertyChanged("MaxGray");
                System.Diagnostics.Trace.WriteLine(string.Format("MaxGray = {0}", maxGray));
            }
        }

        private double minArea;
        public double MinArea
        {
            get { return minArea; }
            set { minArea = value; OnPropertyChanged("MinArea"); }
        }

        private double maxArea;
        public double MaxArea
        {
            get { return maxArea; }
            set { maxArea = value; OnPropertyChanged("MaxArea"); }
        }

        private double areaIC;
        public double AreaIC
        {
            get { return areaIC; }
            set { areaIC = value; OnPropertyChanged("AreaIC"); }
        }

        private double centerRowIC;
        public double CenterRowIC
        {
            get { return centerRowIC; }
            set { centerRowIC = value; OnPropertyChanged("CenterRowIC"); }
        }

        private double centerColIC;
        public double CenterColIC
        {
            get { return centerColIC; }
            set { centerColIC = value; OnPropertyChanged("CenterColIC"); }
        }

        private bool invalidIC = true;

        public bool InvalidIC
        {
            get { return invalidIC; }
            set { invalidIC = value; OnPropertyChanged("InvalidIC"); }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

    }

    class RegionData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Area { get; set; }
    }
}
