using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HalconDotNet;

namespace PCBInspection.Model
{
    class ImageProcessor : IDisposable
    {
        private HWindow windowHandle;

        // Local iconic variables 
        HObject ho_Image, ho_Bright, ho_RectPCB, ho_ImagePCB;
        HObject ho_Regions, ho_RectRegions, ho_ConnectedRegions;
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
            ho_Image.Dispose();
            HOperatorSet.ReadImage(out ho_Image, imagePath);
            HOperatorSet.GetImageSize(ho_Image, out hv_width, out hv_height);
            windowHandle.SetPart(0, 0, hv_height - 1, hv_width - 1);
            windowHandle.DispObj(ho_Image);
            return true;
        }

        public bool LoadImage()
        {
            return this.LoadImage(this.ImagePath);
        }
        #endregion

        //加载模板图片
        #region
        private string modelImagePath = "Image";
        public string ModelImagePath
        {
            get { return modelImagePath; }
            set { modelImagePath = value; }
        }

        public bool LoadModelImage()
        {
            return this.LoadImage(this.ModelImagePath);
        }

        public void ResetModelImageFile()
        {
            this.ModelImagePath = this.ImagePath;
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

        private int maxBackColorValue = 200;
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
        private int minICColorValue = 200;
        public int MinICColorValue
        {
            get { return minICColorValue; }
            set { minICColorValue = value; }
        }

        private int maxICColorValue = 255;
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

        public bool SelectTargetICs()
        {
            ho_RectRegions.Dispose();
            HOperatorSet.OpeningRectangle1(ho_Regions, out ho_RectRegions, 10, 10);  //TODO： 改成膨胀腐蚀

            ho_ConnectedRegions.Dispose();
            HOperatorSet.Connection(ho_RectRegions, out ho_ConnectedRegions);


            ho_SelectedRegions.Dispose();
            HOperatorSet.SelectShapeStd(ho_ConnectedRegions, out ho_SelectedRegions, "rectangle1", this.RectSimilarityValue);
            HOperatorSet.CountObj(ho_SelectedRegions, out hv_RegionCount);
            if (hv_RegionCount <= 0)
            {
                return false;
            }

            if (windowHandle.IsInitialized())
            {
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
            get { return modelFilePath; }
            set { modelFilePath = value; }
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
    }

    class RegionData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Area { get; set; }
    }
}
