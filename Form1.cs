using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;

namespace INFOIBV
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;
        private Bitmap ControlImage;
        private Bitmap SecondImage;
        sbyte[,] Sobelx = new sbyte[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } }; // sobel kernels
        sbyte[,] Sobely = new sbyte[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
        private int Threshold = 8;
        private bool down = true;
        private int MinY; private int MinX;
        private int MaxY; private int MaxX;
        private float[] caviculaLines = new float[4];
        private Point rightUpperBorder = new Point(0,0); private Point leftUpperBorder = new Point(0, 0);
        private Point rightLowerBorder = new Point(0, 0); private Point leftLowerBorder = new Point(0, 0);
        private List<int[]> lungs; 

        public INFOIBV()
        {
            InitializeComponent();
        }

        /*
         * loadButton_Click: process when user clicks "Load" button
         */
        private void loadImageButton_Click(object sender, EventArgs e)
        {
            loadImageButton();
        }

        //Makes loadButton_Click accessible by more methods
        private void loadImageButton()
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // open file dialog
            {
                string file = openImageDialog.FileName;                     // get the file name
                imageFileName.Text = file;                                  // show file name
                if (InputImage != null) InputImage.Dispose();               // reset image
                InputImage = new Bitmap(file);                              // create new Bitmap from file
                if (InputImage.Size.Height <= 0 || InputImage.Size.Width <= 0 ||
                    InputImage.Size.Height > 512 || InputImage.Size.Width > 512) // dimension check (may be removed or altered)
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                {
                    //pictureBox1.Image = (Image)InputImage;                 // display input image
                    Bitmap temp = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
                    Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)

                    // copy input Bitmap to array            
                    for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                        for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                            Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in
                    byte[,] workingImage = convertToGrayscale(Image);

                    for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                        for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                        {
                            Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                            temp.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                        }
                    pictureBox1.Image = (Image)temp;
                }
            }
        }

        /*
         * applyButtonS_Click: process when user clicks one of the "Apply" buttons
         */
        private void applyButtonGaussian_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)

            // copy input Bitmap to array            
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)

            byte[,] workingImage = convertToGrayscale(Image);
            workingImage = adjustContrast(workingImage);
            float[,] gaussianKernel = createGaussianFilter(5, 1);
            workingImage = convolveImage(workingImage, gaussianKernel); ///TODO: use Gaussian?
            workingImage = edgeMagnitude(workingImage, Sobelx, Sobely);
            workingImage = borderDeletion(workingImage, 0.07); //Delete clutter at the edges of the image
            workingImage = thresholdImage(workingImage, Threshold); ///TODO: determine threshold
            for (int t = 0; t <= 5; t++)
            {
                workingImage = noiseSuppressor(workingImage, 3); ///TODO: 6 times?
            }
            tUp(sender, EventArgs.Empty);

            // ==================== END OF YOUR FUNCTION CALLS ====================
            // ====================================================================

            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }

            pictureBox2.Image = (Image)OutputImage;                         // display output image
        }

        private void applyButtonMedian_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)

            byte[,] workingImage = convertToGrayscale(Image);
            workingImage = adjustContrast(workingImage);
            workingImage = medianFilter(workingImage, 3);
            workingImage = edgeMagnitude(workingImage, Sobelx, Sobely);
            workingImage = thresholdImage(workingImage, 23);
            ///TODO: remove and make own button


            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }

            pictureBox2.Image = (Image)OutputImage;                         // display output image
        }

        private void DilateImage_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)


            byte[,] workingImage = convertToGrayscale(Image);
            workingImage = adjustContrast(workingImage);
            // workingImage = thresholdImage(workingImage);
            byte[,] structuringelement = createStructuringElement("plus", 5);
            // workingImage = DilateImageBinary(workingImage, structuringelement);
            workingImage = DilateImageGrayscale(workingImage, structuringelement);
            //
            //Own code till here in this method
            //

            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }
            pictureBox2.Image = (Image)OutputImage;                         // display output image
        }

        private void ErodeImage_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)


            byte[,] workingImage = convertToGrayscale(Image);
            // workingImage = thresholdImage(workingImage);
            byte[,] structuringelement = createStructuringElement("plus", 3);
            workingImage = ErodeImageBinary(workingImage, structuringelement);
            //  workingImage = ErodeImageGrayscale(workingImage, structuringelement);
            //
            //Own code till here in this method
            //

            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }
            pictureBox2.Image = (Image)OutputImage;                         // display output image
        }

        private void OpenImage_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)


            byte[,] workingImage = convertToGrayscale(Image);
            //  workingImage = thresholdImage(workingImage);
            byte[,] structuringelement = createStructuringElement("plus", 5);
            workingImage = OpenImageBinary(workingImage, structuringelement);
            // workingImage = OpenImageGrayscale(workingImage, structuringelement);
            //
            //Own code till here in this method
            //

            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }

            pictureBox2.Image = (Image)OutputImage;                         // display output image
        }

        private void CloseImage_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)


            byte[,] workingImage = convertToGrayscale(Image);
            //  workingImage = thresholdImage(workingImage);
            byte[,] structuringelement = createStructuringElement("plus", 3);
            //  workingImage = CloseImageBinary(workingImage, structuringelement);

            workingImage = CloseImageGrayscale(workingImage, structuringelement);
            //
            //Own code till here in this method
            //

            // copy array to output Bitmap

            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }

            pictureBox2.Image = (Image)OutputImage;                         // display output image
        }

        //AND button
        private void applyButtonAnd_Click(object sender, EventArgs e)
        {

            if (InputImage == null) loadImageButton();                      //if no primary image selected, select one
            loadImage2();                                                   //Load a second image
            if (SecondImage == null || InputImage == null) return;          // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = sizeDetermination(InputImage, SecondImage);       // create new output image
            Color[,] Image1 = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image1[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)
            Color[,] Image2 = new Color[SecondImage.Size.Width, SecondImage.Size.Height]; //Makes second image
            for (int x = 0; x < SecondImage.Size.Width; x++)
                for (int y = 0; y < SecondImage.Size.Height; y++)
                    Image2[x, y] = SecondImage.GetPixel(x, y);


            byte[,] workingImage1 = convertToGrayscale(Image1);          // convert image to grayscale
            byte[,] workingImage2 = convertToGrayscale(Image2);
            byte[,] workingImage = andImage(workingImage1, workingImage2);
            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }
            pictureBox2.Image = (Image)OutputImage;
        }

        //OR button
        private void applyButtonOr_Click(object sender, EventArgs e)
        {
            if (InputImage == null) loadImageButton();                      //if no primary image selected, select one
            loadImage2();                                                   //Load a second image
            if (SecondImage == null || InputImage == null) return;          // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image1 = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image1[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)
            Color[,] Image2 = new Color[SecondImage.Size.Width, SecondImage.Size.Height]; //Makes second image
            for (int x = 0; x < SecondImage.Size.Width; x++)
                for (int y = 0; y < SecondImage.Size.Height; y++)
                    Image2[x, y] = SecondImage.GetPixel(x, y);


            byte[,] workingImage1 = convertToGrayscale(Image1);          // convert image to grayscale
            byte[,] workingImage2 = convertToGrayscale(Image2);
            byte[,] workingImage = orImage(workingImage1, workingImage2);
            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }
            pictureBox2.Image = (Image)OutputImage;
        }
        private void geodesicImageButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) loadImageButton();                      //if no primary image selected, select one
            loadImage2();                                                   //Load a second image
            if (SecondImage == null || InputImage == null) return;          // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image1 = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image1[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)
            Color[,] Image2 = new Color[SecondImage.Size.Width, SecondImage.Size.Height]; //Makes second image
            for (int x = 0; x < SecondImage.Size.Width; x++)
                for (int y = 0; y < SecondImage.Size.Height; y++)
                    Image2[x, y] = SecondImage.GetPixel(x, y);


            byte[,] workingImage1 = convertToGrayscale(Image1);          // convert image to grayscale
            byte[,] workingImage2 = convertToGrayscale(Image2);
            byte[,] structuringelement = createStructuringElement("plus", 5);
            byte[,] workingImage = createGeodesicDilation(workingImage1, workingImage2, structuringelement, 5);
            //  byte[,] workingImage = createGeodesicErosion(workingImage1, workingImage2, structuringelement, 5)
            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }
            pictureBox2.Image = (Image)OutputImage;
        }
        private void applyButtonTest_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)


            byte[,] workingImage = convertToGrayscale(Image);          // convert image to grayscale
            //byte[,] invertedImage = invertImage(workingImage);         // inverts the image
            byte[,] contrastImage = adjustContrast(workingImage);      // Adjusts contrast
            //float[,] gaussianKernel = createGaussianFilter(5, 1);      // Create filter kernel
            //byte[,] gaussianImage = convolveImage(workingImage, gaussianKernel); //Adds gaussian blur
            //byte[,] medianImage = medianFilter(workingImage, 3);       // Adds a median filter
            byte[,] edgeImage = edgeMagnitude(workingImage, Sobelx, Sobely); //Gives the magnitude of edges
            byte[,] thresdImage = thresholdImage(workingImage, 23);         // Thresholds image


            /* D1-D5
            float[,] gaussianKernel3 = createGaussianFilter(3, 1);
            float[,] gaussianKernel5 = createGaussianFilter(5, 3);
            float[,] gaussianKernel7 = createGaussianFilter(7, 5);
            float[,] gaussianKernel9 = createGaussianFilter(9, 7);
            float[,] gaussianKernel11 = createGaussianFilter(11, 9);
            byte[,] gaussian = thresholdImage(edgeMagnitude(convolveImage(workingImage, gaussianKernel11), sobelx, sobely)); */
            workingImage = contrastImage; //Output
            //
            //Own code till here in this method
            //

            // copy array to output Bitmap
            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor = Color.FromArgb(workingImage[x, y], workingImage[x, y], workingImage[x, y]);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }

            pictureBox2.Image = (Image)OutputImage;                         // display output image
        }

        private void CountValues_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)


            byte[,] workingImage = convertToGrayscale(Image);
            int[] histogram = CountValuesHistogram(workingImage);
            for (int s = 0; s < 256; s++)
            {
                CountValuesChart.Series["Number of values"].Points.AddXY(s, histogram[s]);
            }
        }

        private void DistinctValuesButton_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)


            byte[,] workingImage = convertToGrayscale(Image);
            /* int[] DistinctValuesDilation = CountingvaluesDilationSEs(workingImage, 5);
             for(int s = 3; s < DistinctValuesDilation.Length; s+= 2)
             {
                 Console.WriteLine(s.ToString(), DistinctValuesDilation[s].ToString());
             }*/

            int[] NonBackgroundValues = CountingvaluesOpeningSEs(workingImage, 5);
            for (int s = 3; s < NonBackgroundValues.Length; s += 20)
            {
                Console.WriteLine(s.ToString(), NonBackgroundValues[s].ToString());
            }

        }

        private void HoughTransform_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)

            byte[,] workingImage = convertToGrayscale(Image);
            byte[,] showImage = convertToGrayscale(Image);
            workingImage = edgeMagnitude(workingImage, Sobelx, Sobely);
            workingImage = thresholdImage(workingImage, 23);

            ///TODO: parameters
            //Keep kernelHough small for pictures with good edge and kernelPeaks small for dense images
            //and the percentage small for images with few lines and little noise
            List<double[]> peaks = peakFinderFromImage(workingImage, 1, 3, 0.3f);

            List<Tuple<int, int, int, int>> Lines = new List<Tuple<int, int, int, int>>();
            ///TODO: parameters
            //minLength should be small for small images and maxGap should be small for images with little noise 
            Lines = houghlineDetection(workingImage, peaks, 8, 10, 6);
            workingImage = visualizeHoughLineSegments(workingImage, Lines);

            // copy array to output Bitmap

            for (int x = 0; x < workingImage.GetLength(0); x++)             // loop over columns
                for (int y = 0; y < workingImage.GetLength(1); y++)         // loop over rows
                {
                    Color newColor;
                    if (workingImage[x, y] == 0)
                    {
                        newColor = Color.FromArgb(showImage[x, y], showImage[x, y], showImage[x, y]); 
                    }
                    else
                        newColor = Color.FromArgb(255, 0, 0);
                    OutputImage.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                }

            pictureBox2.Image = (Image)OutputImage;                         // display output image

        }

        /*
         * saveButton_Click: process when user clicks "Save" button
        */
        private void saveButton_Click(object sender, EventArgs e)
        {
            if (OutputImage == null) return;                                // get out if no output image
            if (saveImageDialog.ShowDialog() == DialogResult.OK)
                OutputImage.Save(saveImageDialog.FileName);                 // save the output image
        }


        /*
         * convertToGrayScale: convert a three-channel color image to a single channel grayscale image
         * input:   inputImage          three-channel (Color) image
         * output:                      single-channel (byte) image
         */
        private byte[,] convertToGrayscale(Color[,] inputImage)
        {
            // create temporary grayscale image of the same size as input, with a single channel
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // setup progress bar
            progressBar.Visible = true;
            progressBar.Minimum = 1;
            progressBar.Maximum = InputImage.Size.Width * InputImage.Size.Height;
            progressBar.Value = 1;
            progressBar.Step = 1;

            // process all pixels in the image
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                {
                    Color pixelColor = inputImage[x, y];                    // get pixel color
                    byte average = (byte)((pixelColor.R + pixelColor.B + pixelColor.G) / 3); // calculate average over the three channels
                    tempImage[x, y] = average;                              // set the new pixel color at coordinate (x,y)
                    progressBar.PerformStep();                              // increment progress bar
                }

            progressBar.Visible = false;                                    // hide progress bar

            return tempImage;
        }


        // ====================================================================
        // ============= YOUR FUNCTIONS FOR ASSIGNMENT 1 GO HERE ==============
        // ====================================================================


        /*
         * invertImage: invert a single channel (grayscale) image
         * input:   inputImage          single-channel (byte) image
         * output:                      single-channel (byte) image
         */
        private byte[,] invertImage(byte[,] inputImage)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
            {
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                {
                    byte pixelvalue = inputImage[x, y];
                    byte maxValue = 255;
                    byte invert = (byte)(maxValue - pixelvalue);
                    tempImage[x, y] = invert;
                }
            }
            return tempImage;
        }


        /*
         * adjustContrast: create an image with the full range of intensity values used
         * input:   inputImage          single-channel (byte) image
         * output:                      single-channel (byte) image
         */
        private byte[,] adjustContrast(byte[,] inputImage)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            //Check what te highest and lowest values are
            byte highestValue = 0; //sets two pixels as the base, has a no chance it'll overshoot from the start
            byte lowestValue = 255;
            for (int x = 0; x < InputImage.Size.Width; x++)
            {
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    if (inputImage[x, y] > highestValue) highestValue = inputImage[x, y];
                    if (inputImage[x, y] < lowestValue) lowestValue = inputImage[x, y];
                }
            }

            if (highestValue == lowestValue) return inputImage; //returns images with one colour as is

            byte CI = 95; //Confidence interval
            float q = ((100 - CI) / 2) / 100; //0.025 either side
            float aMin = (lowestValue * q); float aMax = (highestValue * (1 - q)); //apply the CI
            /* if (aMin == 0 && aMax == 255)
            {
                MessageBox.Show("Your image has maximum contrast");
                return inputImage;
            } */
            for (int i = 0; i < InputImage.Size.Width; i++) //saves computations when the limit values have already been reached
            {
                for (int j = 0; j < InputImage.Size.Height; j++)
                {
                    byte aResult;
                    byte a = inputImage[i, j];
                    if (a <= aMin) aResult = 0; //values outside the CI get mapped to the nearest CI-limit
                    if (a >= aMax) aResult = 255;
                    if (a >= aMin && a <= aMax)
                    {
                        byte b = (byte)(255 / (aMax - aMin));
                        aResult = (byte)((a - aMin) * b); //uses contrast adjustment formula: (a - alow) * 255 / (ahigh - alow)
                    }
                    else aResult = 0;
                    tempImage[i, j] = aResult;
                }
            }
            return tempImage;
        }


        /*
         * createGaussianFilter: create a Gaussian filter of specific square size and with a specified sigma
         * input:   size                length and width of the Gaussian filter (only odd sizes)
         *          sigma               standard deviation of the Gaussian distribution
         * output:                      Gaussian filter
         */
        private float[,] createGaussianFilter(byte size, float sigma) //make the filter to be used elsewhere
        {
            if (size % 2 == 0) //check the filter has an odd size
            {
                size++; //doesn't reject the size, but makes sure it is always possible
                MessageBox.Show("The filter size you choose was not odd");
            }
            float[,] filter = new float[size, size];
            float sigma2 = sigma * sigma;
            byte centreFilterXY = (byte)((size + 1) / 2); //get both the X and Y coords of the centre (they are the same)
            float sumComponents = 0;

            int X; int Y;
            for (byte tX = 0; tX < size; tX++) //loop over all components in the filter
            {
                for (byte tY = 0; tY < size; tY++)
                {
                    X = centreFilterXY - tX; //distance from current to centre
                    Y = centreFilterXY - tY;
                    int X2 = X * X; int Y2 = Y * Y;
                    float exponentPart = -((X2 + Y2) / (2 * sigma2)); //use the Gaussian formula 
                    //float inversepi = 1 / (2 * (float)Math.PI * sigma2);
                    filter[tX, tY] = (float)Math.Exp(exponentPart);
                    sumComponents += filter[tX, tY];
                }
            }

            float[,] result = new float[size, size];
            for (byte cX = 0; cX < size; cX++) //loop over all components in the filter
            {
                for (byte cY = 0; cY < size; cY++)
                {
                    result[cX, cY] = filter[cX, cY] / sumComponents;
                }
            }
            return result;
        }


        /*
         * convolveImage: apply linear filtering of an input image
         * input:   inputImage          single-channel (byte) image
         *          filter              linear kernel
         * output:                      single-channel (byte) image
         */
        private byte[,] convolveImage(byte[,] inputImage, float[,] filter) //use the filter
        {
            // create temporary grayscale image
            byte[,] outImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];
            byte[,] refImage = inputImage;

            int filterCentreXY = (filter.GetLength(0) - 1) / 2; //centre coord of thge filter
            for (int tX = 0; tX < InputImage.Size.Width; tX++) //loop over all pixels
            {
                for (int tY = 0; tY < InputImage.Size.Height; tY++)
                {
                    //operations on a pixel
                    float valueArray = 0;
                    for (int tFX = 0; tFX < filter.GetLength(0); tFX++) //loop over the filter per pixel
                    {
                        for (int tFY = 0; tFY < filter.GetLength(1); tFY++)
                        {
                            //operations per filter entry
                            int dX = tFX - filterCentreXY; //distance from the centre to the place in the filter
                            int dY = tFY - filterCentreXY;
                            if ((tX + dX >= 0 && tY + dY >= 0) && (tX + dX < InputImage.Size.Width && tY + dY < InputImage.Size.Height))
                            {
                                int newX = tX + dX; int newY = tY + dY;
                                valueArray += refImage[newX, newY] * filter[tFX, tFY];
                            }
                            else
                            {
                                valueArray += refImage[tX, tY] * filter[tFX, tFY]; //Handles borders
                            }
                        }
                    }
                    if (valueArray > 255) valueArray = 255;
                    outImage[tX, tY] = (byte)(valueArray);
                }
            }
            return outImage;
        }


        /*
         * medianFilter: apply median filtering on an input image with a kernel of specified size
         * input:   inputImage          single-channel (byte) image
         *          size                length/width of the median filter kernel
         * output:                      single-channel (byte) image
         */
        public byte Median(List<byte> sortedlist)
        {
            int Length = sortedlist.Count;
            byte medianvalue = 0;
            if (Length % 2 == 0)
            {
                int medianleft = Length / 2;
                int medianright = medianleft + 1;
                byte medianlocationl = sortedlist[medianleft];
                byte medianlocationr = sortedlist[medianright];
                medianvalue = (byte)((medianlocationl + medianlocationr) / 2);
            }
            else
            {
                int medianlocation = (Length / 2) + 1;
                medianvalue = sortedlist[medianlocation];
            }

            return medianvalue;
        }

        private byte[,] medianFilter(byte[,] inputImage, byte size)
        {

            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            float[,] filter = new float[size, size];
            int filterCentreXY = (filter.GetLength(0) - 1) / 2;
            byte radius = (byte)((size - 1) / 2);
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
            {
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows     
                {
                    List<byte> values = new List<byte>();
                    for (int tFX = 0; tFX < filter.GetLength(0); tFX++) //loop over the filter per pixel
                    {
                        for (int tFY = 0; tFY < filter.GetLength(1); tFY++)
                        {

                            int dX = tFX - filterCentreXY; //distance from the centre to the place in the filter
                            int dY = tFY - filterCentreXY;
                            int pixelx; int pixely;
                            pixelx = x - dX;
                            pixely = y - dY;
                            if (pixelx < 0)                                 // leftborder
                                pixelx = 0;
                            if (pixely < 0)                                 // upperborder
                                pixely = 0;
                            if (pixelx > InputImage.Size.Width - 1)
                                pixelx = InputImage.Size.Width - 1;
                            if (pixely > InputImage.Size.Height - 1)
                                pixely = InputImage.Size.Height - 1;
                            byte pixelvalue = inputImage[pixelx, pixely];
                            values.Add(pixelvalue);
                        }

                        values.Sort();                                      // sort the list
                        byte medianvalue = Median(values);                  // calculate median
                        tempImage[x, y] = medianvalue;
                    }
                }
            }

            return tempImage;
        }



        /*
         * edgeMagnitude: calculate the image derivative of an input image and a provided edge kernel
         * input:   inputImage          single-channel (byte) image
         *          horizontalKernel    horizontal edge kernel
         *          virticalKernel      vertical edge kernel
         * output:                      single-channel (byte) image
         */
        private byte[,] edgeMagnitude(byte[,] inputImage, sbyte[,] sobelx, sbyte[,] sobely)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // TODO: add your functionality and checks, think about border handling and type conversion (negative values!)


            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                {
                    int Dx = 0;
                    int Dy = 0;
                    for (int tFX = -1; tFX <= 1; tFX++)
                        for (int tFY = -1; tFY <= 1; tFY++)
                        {
                            int pixelx; int pixely;
                            pixelx = x - tFX;
                            pixely = y - tFY;
                            if (pixelx < 0)                                 // leftborder
                                pixelx = 0;
                            if (pixely < 0)                                 // upperborder
                                pixely = 0;
                            if (pixelx > InputImage.Size.Width - 1)
                                pixelx = InputImage.Size.Width - 1;
                            if (pixely > InputImage.Size.Height - 1)
                                pixely = InputImage.Size.Height - 1;
                            int pixelvalue = inputImage[pixelx, pixely];        // get each pixel in filter region
                            int weightSx = sobelx[tFX + 1, tFY + 1];              // get weight of the pixels
                            int weightedpixelvalueSx = (pixelvalue * weightSx) / 8;
                            Dx += weightedpixelvalueSx;                         // get Dx 
                            int weightSy = sobely[tFX + 1, tFY + 1];
                            int weightedpixelvalueSy = (pixelvalue * weightSy) / 8;
                            Dy += weightedpixelvalueSy;
                        }
                    double Edgestrength1 = Math.Sqrt((Dx * Dx) + (Dy * Dy));
                    byte Edgestrength = (byte)(Math.Round(Edgestrength1));
                    List<byte> test = new List<byte>();
                    test.Add(Edgestrength);
                    if (Edgestrength > 255)
                        Edgestrength = 255;
                    if (Edgestrength < 0)
                        Edgestrength = 0;
                    tempImage[x, y] = Edgestrength;
                }

            return tempImage;
        }


        /*
         * thresholdImage: threshold a grayscale image
         * input:   inputImage          single-channel (byte) image
         * output:                      single-channel (byte) image with on/off values
         */
        //
        //INCLUDE
        //
        private byte[,] thresholdImage(byte[,] inputImage, int threshold)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            int[] histogram = CountValuesHistogram(inputImage);
            int minValue = 255; int maxValue = 0;
            for(int i = 0; i < histogram.Length; i++)
            {
                if (histogram[i] > 0 && i > maxValue) maxValue = i;
                if (histogram[i] > 0 && i < minValue) minValue = i;
            }
            threshold = (int)((maxValue - minValue) / Math.PI); ///TODO: fine-tune?
            Threshold = threshold;

            // TODO: add your functionality and checks, think about how to represent the binary values
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
            {
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                {
                    byte pixelvalue = inputImage[x, y];
                    if (pixelvalue < threshold) tempImage[x, y] = 0;
                    else tempImage[x, y] = 255;
                }
            }
            return tempImage;
        }


        // ====================================================================
        // ============= YOUR FUNCTIONS FOR ASSIGNMENT 2 GO HERE ==============
        // ====================================================================

        private byte[,] createStructuringElement(string shape, int size)
        {
            if (size % 2 == 0) //check the filter has an odd size
            {
                size++; //doesn't reject the size, but makes sure it is always possible
                MessageBox.Show("The structuringelement size you choose was not odd");
            }
            byte[,] structuringelement = new byte[size, size];
            byte[,] basicSE = new byte[3, 3];
            if (shape == "square")                              // all values must be 1 
            {
                for (int x = 0; x < size; x++)
                    for (int y = 0; y < size; y++)
                    {
                        structuringelement[x, y] = 1;
                    }
            }

            else if (shape == "plus")
            {
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 3; y++)
                    {
                        if (x == 1 || y == 1)         // the center values must be 1
                            basicSE[x, y] = 1;
                        else
                            basicSE[x, y] = 0;
                    }

                // structuringelement = basicSE;
                if (size > 3)
                {
                    //int m = 5;
                    for (int x = 0; x < size; x++)
                        for (int y = 0; y < size; y++)
                        {
                            if (x == size / 2 || y == size / 2)
                                structuringelement[x, y] = 1;
                            else
                                structuringelement[x, y] = 0;

                        }
                    for (int s = size; s > 3; s -= 2)
                    {
                        //byte[,] workingSE = new byte[m, m];
                        structuringelement = DilateImageBinary(structuringelement, basicSE);
                        //   m += 2;
                        //   Console.WriteLine(s);
                        //    structuringelement = workingSE;
                    }
                }
                else
                    structuringelement = basicSE;


            }

            return structuringelement;
        }

        private byte[,] redefineSE(byte[,] structuringelement)
        {
            byte[,] redefinedSE = new byte[structuringelement.GetLength(0), structuringelement.GetLength(1)];
            for (int x = 0; x < structuringelement.GetLength(0); x++)
                for (int y = 0; y < structuringelement.GetLength(1); y++)
                {
                    if (structuringelement[x, y] == 0)          // all zero values in binary image SE must be null (negative) values in grayscale image SE
                        redefinedSE[x, y] = 255;
                    else                                        // all other values must be 0 
                        redefinedSE[x, y] = 0;
                }

            return redefinedSE;
        }
        private byte[,] DilateImageBinary(byte[,] inputImage, byte[,] structuringelement)
        {

            byte[,] dilatedimage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)]; // create temporary image
            for (int x = 0; x < inputImage.GetLength(0); x++)
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    int radius = structuringelement.GetLength(0) / 2;
                    if (inputImage[x, y] == 0)
                        dilatedimage[x, y] = 0;
                    else if (inputImage[x, y] > 0)                                                    // only dilate foreground values 
                    {
                        dilatedimage[x, y] = 1;                                                // copy image to temporary image
                        for (int fx = 0; fx < structuringelement.GetLength(0); fx++)
                            for (int fy = 0; fy < structuringelement.GetLength(1); fy++)
                            {
                                if (structuringelement[fx, fy] > 0)
                                {
                                    int locx = x - radius + fx;                                // location of SE value in inputimage image 
                                    int locy = y - radius + fy;
                                    if (locx >= 0 && locy >= 0 && locx < inputImage.GetLength(0) && locy < inputImage.GetLength(1))                                // don't use out of boundary values 
                                    {
                                        dilatedimage[locx, locy] = 1;
                                    }
                                }
                            }
                    }
                }

            for (int x = 0; x < dilatedimage.GetLength(0); x++)
                for (int y = 0; y < dilatedimage.GetLength(1); y++)
                {
                    dilatedimage[x, y] *= 255;
                }
            return dilatedimage;
        }

        private byte[,] DilateImageGrayscale(byte[,] inputImage, byte[,] structuringelement)
        {
            // create temporary grayscale image
            byte[,] dilatedImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];
            byte[,] redefinedSE = redefineSE(structuringelement);                             // redefine binary SE to grayscale SE
            for (int x = 0; x < InputImage.Size.Width; x++)
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    List<byte> values = new List<byte>();
                    int radius = structuringelement.GetLength(0) / 2;
                    for (int fx = 0; fx < redefinedSE.GetLength(0); fx++)
                        for (int fy = 0; fy < redefinedSE.GetLength(1); fy++)
                        {
                            if (redefinedSE[fx, fy] < 255)                             // don't use null values 
                            {
                                int locx = x - radius + fx;
                                int locy = y - radius + fy;
                                if (locx >= 0 && locy >= 0 && locx < inputImage.GetLength(0) && locy < inputImage.GetLength(1))
                                {
                                    byte value = (byte)(inputImage[locx, locy] + redefinedSE[fx, fy]); // dilate SE from inputImage
                                    values.Add(value);                                         // save all values to a list 
                                }
                            }

                        }
                    byte maxvalue = values.Max();
                    dilatedImage[x, y] = maxvalue;                                          // get the max value from all the values that are "on" in SE
                }

            return dilatedImage;
        }

        private byte[,] ErodeImageBinary(byte[,] inputImage, byte[,] structuringelement)
        {
            byte[,] erodedimage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)]; // create temp image
            for (int x = 0; x < InputImage.Size.Width; x++)
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    int radius = structuringelement.GetLength(0) / 2;
                    if (inputImage[x, y] == 0)
                        erodedimage[x, y] = 0;
                    else if (inputImage[x, y] > 0)
                    {
                        erodedimage[x, y] = 1;
                        for (int fx = 0; fx < structuringelement.GetLength(0); fx++)
                            for (int fy = 0; fy < structuringelement.GetLength(1); fy++)
                            {
                                int locx = x - radius + fx;
                                int locy = y - radius + fy;
                                if (locx >= 0 && locy >= 0 && locx < inputImage.GetLength(0) && locy < inputImage.GetLength(1))
                                    if (structuringelement[fx, fy] > 0 && inputImage[locx, locy] == 0) //remove value if the structuring element is not covering the image 
                                    {
                                        erodedimage[x, y] = 0;
                                    }
                            }
                    }
                }

            for (int x = 0; x < erodedimage.GetLength(0); x++)
                for (int y = 0; y < erodedimage.GetLength(1); y++)
                {
                    erodedimage[x, y] *= 255;
                }
            return erodedimage;
        }

        private byte[,] ErodeImageGrayscale(byte[,] inputImage, byte[,] structuringelement)
        {
            // create temporary grayscale image
            byte[,] erodedImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];
            byte[,] redefinedSE = redefineSE(structuringelement);
            for (int x = 0; x < InputImage.Size.Width; x++)
                for (int y = 0; y < InputImage.Size.Height; y++)
                {
                    List<byte> values = new List<byte>();
                    int radius = structuringelement.GetLength(0) / 2;
                    for (int fx = 0; fx < structuringelement.GetLength(0); fx++)
                        for (int fy = 0; fy < structuringelement.GetLength(1); fy++)
                        {
                            if (structuringelement[fx, fy] < 255)
                            {
                                int locx = x - radius + fx;
                                int locy = y - radius + fy;
                                if (locx >= 0 && locy >= 0 && locx < inputImage.GetLength(0) && locy < inputImage.GetLength(1))
                                {
                                    byte value = (byte)(inputImage[locx, locy] - redefinedSE[fx, fy]);
                                    values.Add(value);
                                }
                            }
                        }
                    erodedImage[x, y] = values.Min();
                }
            return erodedImage;
        }

        private byte[,] OpenImageBinary(byte[,] inputImage, byte[,] structuringelement)
        {
            byte[,] erodedImage = ErodeImageBinary(inputImage, structuringelement);
            byte[,] dilatedImage = DilateImageBinary(erodedImage, structuringelement);

            return dilatedImage;
        }

        private byte[,] OpenImageGrayscale(byte[,] inputImage, byte[,] structuringelement)
        {
            byte[,] erodedImage = ErodeImageGrayscale(inputImage, structuringelement);
            byte[,] dilatedImage = DilateImageGrayscale(erodedImage, structuringelement);

            return dilatedImage;
        }

        private byte[,] CloseImageBinary(byte[,] inputImage, byte[,] structuringelement)
        {
            byte[,] dilatedImage = DilateImageBinary(inputImage, structuringelement);
            byte[,] erodedImage = ErodeImageBinary(dilatedImage, structuringelement);

            return erodedImage;
        }

        private byte[,] CloseImageGrayscale(byte[,] inputImage, byte[,] structuringelement)
        {
            byte[,] dilatedImage = DilateImageGrayscale(inputImage, structuringelement);
            byte[,] erodedImage = ErodeImageGrayscale(dilatedImage, structuringelement);

            return erodedImage;
        }

        private byte[,] createGeodesicDilation(byte[,] inputImage, byte[,] controlImage, byte[,] structuringelement, int numberofdilations)
        {
            byte[,] firstimage = DilateImageBinary(inputImage, structuringelement);
            for (int i = 0; i < (numberofdilations - 1); i++)
                firstimage = DilateImageBinary(firstimage, structuringelement);
            for (int x = 0; x < firstimage.GetLength(0); x++)
                for (int y = 0; y < firstimage.GetLength(1); y++)
                {
                    if (firstimage[x, y] > 0 && controlImage[x, y] < 1)
                        firstimage[x, y] = 0;
                }

            return firstimage;
        }

        private byte[,] createGeodesicDilationGrayscale(byte[,] inputImage, byte[,] controlImage, byte[,] structuringelement, int numberofdilations)
        {
            byte[,] firstimage = DilateImageGrayscale(inputImage, structuringelement);
            for (int i = 0; i < (numberofdilations - 1); i++)
                firstimage = DilateImageGrayscale(firstimage, structuringelement);
            for (int x = 0; x < firstimage.GetLength(0); x++)
                for (int y = 0; y < firstimage.GetLength(1); y++)
                {
                    if (firstimage[x, y] > controlImage[x, y])
                        firstimage[x, y] = controlImage[x, y];
                }

            return firstimage;
        }

        private byte[,] createGeodesicErosion(byte[,] inputImage, byte[,] controlImage, byte[,] structuringelement, int numberoferosions)
        {
            byte[,] firstimage = ErodeImageBinary(inputImage, structuringelement);
            for (int i = 0; i < (numberoferosions - 1); i++)
                firstimage = ErodeImageBinary(firstimage, structuringelement);
            for (int x = 0; x < firstimage.GetLength(0); x++)
                for (int y = 0; y < firstimage.GetLength(1); y++)
                {
                    if (firstimage[x, y] < 1 && controlImage[x, y] > 0)
                        firstimage[x, y] = 255;
                }
            return firstimage;
        }

        private byte[,] createGeodesicErosionGrayscale(byte[,] inputImage, byte[,] controlImage, byte[,] structuringelement, int numberoferosions)
        {
            byte[,] firstimage = ErodeImageBinary(inputImage, structuringelement);
            for (int i = 0; i < (numberoferosions - 1); i++)
                firstimage = ErodeImageBinary(firstimage, structuringelement);
            for (int x = 0; x < firstimage.GetLength(0); x++)
                for (int y = 0; y < firstimage.GetLength(1); y++)
                {
                    if (firstimage[x, y] < controlImage[x, y])
                        firstimage[x, y] = controlImage[x, y];
                }
            return firstimage;
        }

        private byte[,] showbinaryimage(byte[,] inputImage)
        {
            for (int x = 0; x < inputImage.GetLength(0); x++)
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    inputImage[x, y] *= 255;
                }
            return inputImage;
        }

        //Creates a binary image from a thresholded one
        private byte[,] makeBinaryImage(byte[,] inputImage)
        {
            inputImage = thresholdImage(inputImage, 23);

            for (int x = 0; x < inputImage.GetLength(0); x++)
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    inputImage[x, y] /= 255;
                }
            return inputImage;
        }

        //opening a second image
        private void loadImage2()
        {
            if (openImageDialog.ShowDialog() == DialogResult.OK)             // open file dialog
            {
                string file = openImageDialog.FileName;                     // get the file name
                if (SecondImage != null) SecondImage.Dispose();              // reset image

                SecondImage = new Bitmap(file);                              // create new Bitmap from file
                if (SecondImage.Size.Height <= 0 || SecondImage.Size.Width <= 0 ||
                    SecondImage.Size.Height > 512 || SecondImage.Size.Width > 512) // dimension check (may be removed or altered)
                    MessageBox.Show("Error in image dimensions (have to be > 0 and <= 512)");
                else
                {
                    Bitmap temp = new Bitmap(SecondImage.Size.Width, SecondImage.Size.Height); // create new output image
                    Color[,] Image = new Color[SecondImage.Size.Width, SecondImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)

                    // copy input Bitmap to array            
                    for (int x = 0; x < SecondImage.Size.Width; x++)                 // loop over columns
                        for (int y = 0; y < SecondImage.Size.Height; y++)            // loop over rows
                            Image[x, y] = SecondImage.GetPixel(x, y);                // set pixel color in
                    byte[,] secondImage = convertToGrayscale(Image);

                    for (int x = 0; x < secondImage.GetLength(0); x++)             // loop over columns
                        for (int y = 0; y < secondImage.GetLength(1); y++)         // loop over rows
                        {
                            Color newColor = Color.FromArgb(secondImage[x, y], secondImage[x, y], secondImage[x, y]);
                            temp.SetPixel(x, y, newColor);                  // set the pixel color at coordinate (x,y)
                        }
                    SecondImage = temp;
                }
            }
        }

        //AND-ing 2 images
        private byte[,] andImage(byte[,] inputImage1, byte[,] inputImage2)
        {
            // create temporary grayscale image
            byte[,] resultImage = sizeDetermination(inputImage1, inputImage2);

            //if the images do not have an equal size the remaining pixels are discarded
            for (int x = 0; x < resultImage.GetLength(0); x++)
            {
                for (int y = 0; y < resultImage.GetLength(1); y++)
                {
                    if (inputImage1[x, y] > 0 && inputImage2[x, y] > 0) resultImage[x, y] = 1; //AND operation
                    else resultImage[x, y] = 0;
                }
            }

            resultImage = showbinaryimage(resultImage);

            return resultImage;
        }

        //OR-ing 2 images
        private byte[,] orImage(byte[,] inputImage1, byte[,] inputImage2)
        {
            // create temporary grayscale image
            byte[,] resultImage = sizeDetermination(inputImage1, inputImage2);

            //if the images do not have an equal size the remaining pixels are discarded
            for (int x = 0; x < resultImage.GetLength(0); x++)
            {
                for (int y = 0; y < resultImage.GetLength(1); y++)
                {
                    if (inputImage1[x, y] == 0 && inputImage2[x, y] == 0) resultImage[x, y] = 0; //only "0 OR 0" results in 0
                    else resultImage[x, y] = 1;
                }
            }

            resultImage = showbinaryimage(resultImage);
            return resultImage;
        }

        //Determining the size of the AND/OR-ed image
        private byte[,] sizeDetermination(byte[,] inputImage1, byte[,] inputImage2)
        {
            int lengthX = 0; int lengthY = 0;
            if (inputImage1.GetLength(0) >= inputImage2.GetLength(0)) lengthX = inputImage2.GetLength(0);
            if (inputImage1.GetLength(0) < inputImage2.GetLength(0)) lengthX = inputImage1.GetLength(0);
            if (inputImage1.GetLength(1) >= inputImage2.GetLength(1)) lengthY = inputImage2.GetLength(1);
            if (inputImage1.GetLength(1) < inputImage2.GetLength(1)) lengthY = inputImage1.GetLength(1);
            byte[,] resultImage = new byte[lengthX, lengthY];
            return resultImage;
        }

        //Determines the size for a Bitmap AND/OR-ed
        private Bitmap sizeDetermination(Bitmap inputImage1, Bitmap inputImage2)
        {
            int lengthX = 0; int lengthY = 0;
            if (inputImage1.Size.Width >= inputImage2.Size.Width) lengthX = inputImage2.Size.Width;
            if (inputImage1.Size.Width < inputImage2.Size.Width) lengthX = inputImage1.Size.Width;
            if (inputImage1.Size.Height >= inputImage2.Size.Height) lengthY = inputImage2.Size.Height;
            if (inputImage1.Size.Height < inputImage2.Size.Height) lengthY = inputImage1.Size.Height;
            Bitmap resultImage = new Bitmap(lengthX, lengthY);
            return resultImage;
        }

        //Create a chart of all values and their prevalence
        private int[] CountValuesHistogram(byte[,] inputImage) // counting how often each value occurs, we have decided to split the method into two methods. This one will give a array to create the histogram
        {
            int[] histogram = new int[256];


            for (int x = 0; x < inputImage.GetLength(0); x++)
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    byte pixelValue = inputImage[x, y];
                    histogram[pixelValue]++;
                }
            }

            return histogram;

        }

        private int CountdistinctValues(int[] histogram) // this method will give the number of distinct values 
        {
            int uniqueValues = 0;

            for (int t = 0; t < histogram.Length; t++)
            {
                if (histogram[t] != 0) uniqueValues++;
            }

            return uniqueValues;
        }

        private int[] CountingvaluesDilationSEs(byte[,] inputImage, int numberofdilations)
        {
            int maxSE = 3 + ((numberofdilations - 1) * 2);
            List<byte[,]> differentImages = new List<byte[,]>();
            int[] distinctvalues = new int[maxSE + 1];
            int startposition = 3;
            for (int s = 3; s <= maxSE; s += 2)
            {
                byte[,] structuringelement = createStructuringElement("square", s);
                byte[,] Image = DilateImageGrayscale(inputImage, structuringelement);
                differentImages.Add(Image);
            }

            for (int n = 0; n < differentImages.Count; n++)
            {
                int[] histogram = CountValuesHistogram(differentImages[n]);
                int value = CountdistinctValues(histogram);
                distinctvalues[startposition] = value;
                startposition += 2;
            }

            return distinctvalues;
        }

        private int[] CountingvaluesOpeningSEs(byte[,] inputImage, int numberofdilations)
        {
            int maxSE = 3 + ((numberofdilations - 1) * 20);
            List<byte[,]> differentImages = new List<byte[,]>();
            int[] nonbackgroundvalues = new int[maxSE + 1];
            int startposition = 3;
            for (int s = 3; s <= maxSE; s += 20)
            {
                byte[,] structuringelement = createStructuringElement("square", s);
                byte[,] Image = OpenImageBinary(inputImage, structuringelement);
                differentImages.Add(Image);
            }

            for (int n = 0; n < differentImages.Count; n++)
            {
                for (int x = 0; x < inputImage.GetLength(0); x++)
                    for (int y = 0; y < inputImage.GetLength(1); y++)
                    {
                        byte[,] usingImage = differentImages[n];
                        if (usingImage[x, y] > 0)
                        {
                            nonbackgroundvalues[startposition]++;
                        }
                    }
                startposition += 20;
            }



            return nonbackgroundvalues;
        }

        private List<int[]> traceBoundary(byte[,] binaryImage)
        {
            List<int[]> boundaryList = new List<int[]>();
            int[] startPoint = new int[2];
            bool startPointFound = false;

            //Find the first object you come across
            for (int x = 0; x < binaryImage.GetLength(0) && !startPointFound; x++)
            {
                for (int y = 0; y < binaryImage.GetLength(1) && !startPointFound; y++)
                {
                    if (binaryImage[x, y] == 1)
                    {
                        startPoint[0] = x; startPoint[1] = y;
                        startPointFound = true;
                        binaryImage[x, y] = 2;
                        if (x == 0 || y == 0
                            || x == binaryImage.GetLength(0) - 1 || y == binaryImage.GetLength(1) - 1)
                        {
                            MessageBox.Show("Your object is on the edge of the image and can not fully be traced");
                            return boundaryList;
                        }
                    }
                }
            }
            if (!startPointFound) //In case of an "empty" image
            {
                MessageBox.Show("There is no object in your binary image.");
                return boundaryList;
            }

            //The tracing
            bool tracingComplete = false;
            int[] currentPoint = startPoint; //keep track of the current pixel
            int[] previousPoint = currentPoint; //keep track of the last step
            List<byte[]> neighbourVectors = new List<byte[]>()
                {
                    new byte[] {0, 0, 1}, new byte[] {1, 0, 2}, new byte[] {2, 0, 3},
                    new byte[] {2, 1, 4}, new byte[] {2, 2, 3}, new byte[] {1, 2, 2},
                    new byte[] {0, 2, 1}, new byte[] { 0, 1, 0} //list of all neighbouring pixels (as vector movements from the bottom-right) and their weight
                };
            //Loop to make all steps
            while (!tracingComplete)
            {
                int whileTracker = 0;

                int u = currentPoint[0]; int v = currentPoint[1];
                int trU = u + 1; int trV = v + 1; //the coords of the pixel Bottom-Right of the subject

                int[] stepMatrix = { -1, -1, 7 }; //0 = x, 1 = y, 2 = weight (7 being the lowest)
                bool unSteppedPixelAvaible = false; //reset per step taken
                bool moveToBeginPossible = true;
                //Code to determine the next pixel to look at 
                for (int t = 0; t <= neighbourVectors.Count; t++) //<= to get a chance to look at all options
                {
                    int stepX = trU - neighbourVectors[t][0];
                    int stepY = trV - neighbourVectors[t][1];
                    if ((stepX >= 0 && stepY >= 0
                        && stepX <= binaryImage.GetLength(0) && stepX <= binaryImage.GetLength(1))
                        && t != neighbourVectors.Count) //makes sure the step is possible to look at and not already done
                    {
                        //Is it foreground and what type applies
                        //does not consider stepped on pixels when on the start to prevent retracing 
                        if (binaryImage[u, v] == 2
                            || binaryImage[u, v] == 12
                            && (binaryImage[stepX, stepY] != 1))
                        {
                            continue;
                        }
                        //the pixel is an unstepped pixel
                        if (binaryImage[stepX, stepY] == 1)
                        {
                            int tNext = t + 1;
                            int tPrevious = t - 1;
                            if (tNext > 7) tNext = 0;           //Loop it around in the  list
                            if (tPrevious < 0) tPrevious = 7;
                            if (neighbourVectors[tNext][0] >= 0 || neighbourVectors[tNext][1] >= 0)
                            {
                                //Check if it's a possible candidate for the next step
                                if (binaryImage[neighbourVectors[tNext][0], neighbourVectors[tNext][1]] == 0 ||
                                    binaryImage[neighbourVectors[tPrevious][0], neighbourVectors[tPrevious][1]] == 0)
                                {
                                    if ((neighbourVectors[t][2] % 8) < (stepMatrix[2] % 8))
                                    {
                                        stepMatrix[0] = stepX; stepMatrix[1] = stepY;
                                        stepMatrix[2] = neighbourVectors[t][2];
                                        unSteppedPixelAvaible = true;
                                        moveToBeginPossible = false;
                                    }
                                }
                            }
                            if (stepMatrix[2] % 8 == 0) //the right middle neighbour always takes precedent
                            {
                                t = neighbourVectors.Count;
                            }
                        }
                        //the pixel is stepped on once or twice but this is the previous
                        else if ((binaryImage[stepX, stepY] == 3 || binaryImage[stepX, stepY] == 4)
                                 && (stepMatrix[0] == previousPoint[0] && stepMatrix[1] == previousPoint[1])
                                 && !unSteppedPixelAvaible)
                        {
                            if (stepMatrix[0] == -1) //this means there is no possible other step
                            {
                                stepMatrix[0] = stepX; stepMatrix[1] = stepY;
                                stepMatrix[2] = 7; //Lowest weight possible, only a last resort step
                            }
                        }
                        //the pixel is already stepped on once
                        else if (binaryImage[stepX, stepY] == 3 && !unSteppedPixelAvaible)
                        {
                            //only do this when all option have been considered
                            if (stepMatrix[2] % 8 > 5)
                            {
                                stepMatrix[0] = stepX; stepMatrix[1] = stepY;
                                stepMatrix[2] = 5; //set weight slightly "higher"
                                moveToBeginPossible = false;
                            }
                        }
                        //the pixel is stepped on twice already
                        else if (binaryImage[stepX, stepY] == 4 && !unSteppedPixelAvaible)
                        {
                            if (stepMatrix[2] % 8 > 5) //check if you can restep on a time-stepped pixel, which has a preference over this one
                            {
                                stepMatrix[0] = stepX; stepMatrix[1] = stepY;
                                stepMatrix[2] = 6;
                            }
                        }
                        //the pixel is the begin pixel (only as a last resort, but better than undoing the previous step)
                        else if ((binaryImage[stepX, stepY] == 2
                                  || binaryImage[stepX, stepY] == 12) && moveToBeginPossible)
                        {
                            if (stepMatrix[2] % 8 > 6)
                            {
                                stepMatrix[0] = stepX; stepMatrix[1] = stepY;
                                stepMatrix[2] = 7; //set weight slightly "lower" than a stepped pixel
                            }
                        }

                    }
                    //Make a decision when all steps have been evaluated and weighted
                    if (t >= neighbourVectors.Count)
                    {
                        if (stepMatrix[0] == -1) return boundaryList; //end the loop if no valid pixels can be found
                        //Marking and moving on
                        if (binaryImage[currentPoint[0], currentPoint[1]] == 1)
                            binaryImage[currentPoint[0], currentPoint[1]] = 3; //3 means it has been passed
                        else if (binaryImage[currentPoint[0], currentPoint[1]] == 3)
                            binaryImage[currentPoint[0], currentPoint[1]] = 4; //Has been passed twice (+1)
                        else if (binaryImage[currentPoint[0], currentPoint[1]] == 4)
                            binaryImage[currentPoint[0], currentPoint[1]] = 5; //has been passed thrice, and can not be passed more
                        else if (binaryImage[currentPoint[0], currentPoint[1]] == 2)
                            binaryImage[currentPoint[0], currentPoint[1]] = 12; //If the start is stepped on it gets +10
                        else if (binaryImage[currentPoint[0], currentPoint[1]] == 12)
                            binaryImage[currentPoint[0], currentPoint[1]] = 22; //Since no pixel can be right of or above the starting image, this is the end

                        //Make the step
                        boundaryList.Add(currentPoint);
                        previousPoint = currentPoint;
                        currentPoint[0] = stepMatrix[0]; currentPoint[1] = stepMatrix[1]; //Go to the next pixel
                        stepMatrix[0] = -1; stepMatrix[1] = -1; stepMatrix[2] = 7; //Reset the stepMatrix

                        //Definete end point reached
                        if (binaryImage[currentPoint[0], currentPoint[1]] == 22) return boundaryList; //if the start point has been stepped on twice it can't go in a new direction
                    }
                    //END OF FOR (next pixel found)
                }

                whileTracker++;
                //the perimetre of the image is the max boundary, so when reached end the while
                if (whileTracker > 4 * 512)
                {
                    tracingComplete = true;
                }
                //END OF WHILE
            }

            return boundaryList;

        }

        //Get rids of everything but the largest object(s)

        private byte[,] smallRemover(byte[,] inputImage)
        {
            byte[,] workingImage = inputImage;
            byte[,] previousimage = inputImage;
            int stepCounter = 0;
            byte[,] SE = createStructuringElement("square", 3);

            bool notFarEnough = true; //True to start the loop
            while (notFarEnough)
            {
                notFarEnough = false; //Assume it is empty until proven otherwise

                //Check if there are objects in the scene
                for (int i = 0; i < inputImage.GetLength(0); i++)
                {
                    for (int j = 0; j < inputImage.GetLength(1); j++)
                    {
                        if (workingImage[i, j] == 1)
                        {
                            notFarEnough = true;
                        }
                    }
                }

                //Erode more if there still is something in the image
                if (notFarEnough)
                {
                    previousimage = workingImage; //Save the last image
                    workingImage = ErodeImageBinary(workingImage, SE);
                    stepCounter++;
                }
            }



            if (!notFarEnough)
            {
                workingImage = previousimage;
                for (int t = 0; t < stepCounter; t++)
                {
                    workingImage = DilateImageBinary(workingImage, SE); //resize the last image with content
                }
            }
            return workingImage;
        }


        // ====================================================================
        // ============= YOUR FUNCTIONS FOR ASSIGNMENT 3 GO HERE ==============
        // ====================================================================

        private int[,] houghTransform(byte[,] inputImage, int kernelSize)
        {
            int maxR = (int)Math.Sqrt((inputImage.GetLength(0) * inputImage.GetLength(0))
                       + (inputImage.GetLength(1) * inputImage.GetLength(1))); //The max value r can be
            int rangeR = 2 * maxR; //r can be positive and negative
            int[,] transform = new int[inputImage.GetLength(0), rangeR];

            for (int x = 0; x < inputImage.GetLength(0); x++)
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    if (inputImage[x, y] == 1 || inputImage[x, y] == 255) //both "binary forms" arre accepted
                    {
                        //graph the hough transform per pixel
                        double thetaIncrement = 2 * Math.PI / inputImage.GetLength(0); //make theta easily drawable in an image
                        for (double theta = 0; theta < Math.PI; theta += thetaIncrement)
                        {
                            //calculate r for every theta
                            double xPart = x * Math.Cos(theta);
                            double yPart = y * Math.Sin(theta);
                            int r = (int)(xPart + yPart); ///TODO: aanpassing
                            int INTtheta = (int)((inputImage.GetLength(0) * theta) / Math.PI);
                            int INTr = (int)r + maxR;
                            transform[INTtheta, INTr] += 1; //Vote for the found value

                            //Also vote for the values around it
                            int kernelLimit = (int)(0.5 * kernelSize); //Spread of the voting
                            for (int dtheta = -kernelLimit; dtheta <= kernelLimit; dtheta++)
                            {
                                for (int dr = -kernelLimit; dr <= kernelLimit; dr++)
                                {
                                    INTtheta += dtheta;
                                    INTr += dr;
                                    if (INTr > 0 && INTtheta > 0
                                        && INTr < transform.GetLength(1) && INTtheta < transform.GetLength(0))
                                        transform[INTtheta, INTr] += 1; //The centre will be voted for stronger
                                }
                            }
                        }
                    }
                    else if (inputImage[x, y] > 1 && inputImage[x, y] < 255)
                    {
                        inputImage = makeBinaryImage(thresholdImage(inputImage, 23));
                    }
                }
            }

            //accumulator array: make it image size so it can be displayed

            return transform;
        }

        //This function can draw the graph houghTransform creates
        private byte[,] drawHoughTransform(int[,] transform, byte[,] inputImage) ///TODO: fix so it fits
        {
            byte[,] resultImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            //Find the max vale
            int max = 1;
            for (int x = 0; x < transform.GetLength(0); x++)
            {
                for (int y = 0; y < transform.GetLength(1); y++)
                {
                    if (transform[x, y] >= max) transform[x, y] = max;
                }
            }
            float factor = 255 / max; //Factor to scale all values, so it worls with greyscale

            float factorY = (float)(transform.GetLength(1) / inputImage.GetLength(1));
            for (int x = 0; x < transform.GetLength(0); x++)
            {
                for (int y = 0; y < transform.GetLength(1) && y / factorY < inputImage.GetLength(1); y++)
                {
                    resultImage[x, (int)(y / factorY)] = (byte)(transform[x, y] * factor);
                }
            }

            return resultImage;
        }

        //Finds the peaks in the transform
        private List<double[]> peakFinder(int[,] transform, byte[,] inputImage, int kernelSize, float percentageOfMaxStrength)
        {
            List<double[]> results = new List<double[]>();

            //determine the threshold from the most voted for entry
            int MaxStrength = 0;
            for (int x = 0; x < transform.GetLength(0); x++)
            {
                for (int y = 0; y < transform.GetLength(1); y++)
                {
                    if (transform[x, y] > MaxStrength) MaxStrength = transform[x, y];
                }
            }
            int threshold = (int)(percentageOfMaxStrength * MaxStrength);

            //Image level
            for (int i = 0; i < transform.GetLength(0); i++)
            {
                for (int j = 0; j < transform.GetLength(1); j++)
                {
                    //Pixel level
                    if (transform[i, j] >= threshold)
                    {
                        //Check if there is a better line nearby
                        int bestValue = transform[i, j];
                        int kernelLimit = (int)(0.5 * kernelSize); //Decodes the voting spread
                        for (int dI = -kernelLimit; dI <= kernelLimit; dI++)
                        {
                            for (int dJ = -kernelLimit; dJ <= kernelLimit; dJ++)
                            {
                                //Kernel pixel level
                                if (!(dI == 0 && dJ == 0))
                                {
                                    int nI = i + dI; int nJ = j + dJ; //make sure the original i and j remain;
                                    if (nI > 0 && nJ > 0
                                        && nI < transform.GetLength(0) && nJ < transform.GetLength(1))
                                    {
                                        if (transform[nI, nJ] > bestValue) bestValue = transform[nI, nJ];
                                    }
                                }
                            }
                        }

                        if (bestValue == transform[i, j]) //the pixel is the best in the neighbourhood
                        {
                            double theta;
                            int INTtheta = i; int INTr = j;
                            theta = INTtheta * Math.PI / inputImage.GetLength(0); //decode theta from its storable value
                            double[] listPart = new double[] { theta, INTr };
                            results.Add(listPart);
                        }
                    }
                }
            }
            return results;
        }

        //Takes an image and does the transform and returns the peaks
        private List<double[]> peakFinderFromImage(byte[,] inputImage, int kernelSizeHough, int kernelSizePeaks, float percentageOfMaxStrength)
        {
            int[,] transform = houghTransform(inputImage, kernelSizeHough);
            List<double[]> results = new List<double[]>();
            results = peakFinder(transform, inputImage, kernelSizePeaks, percentageOfMaxStrength);
            return results;
        }

        //Finds the lines that should be drawn
        private List<Tuple<int, int, int, int>> houghlineDetection(byte[,] edgeImage, List<double[]> peaks, int threshold, int minLength, int maxGap)
        {
            List<Tuple<int, int, int, int>> Lines = new List<Tuple<int, int, int, int>>();
            byte[,] outputImage = edgeImage;

            int maxR = (int)Math.Sqrt((edgeImage.GetLength(0) * edgeImage.GetLength(0))
                       + (edgeImage.GetLength(1) * edgeImage.GetLength(1))); //The max value r can be
            for (int i = 0; i < peaks.Count; i++)
            {
                int xstart = -1;
                int ystart = -1;
                int xend = -1;
                int yend = -1;
                double theta = peaks[i][0];
                int rho1 = (int)peaks[i][1];
                int rho = rho1 - maxR;
                for (int x = 0; x < edgeImage.GetLength(0); x++)
                    for (int y = 0; y < edgeImage.GetLength(1); y++)
                    {
                        if (outputImage[x, y] > threshold)
                        {
                            double Xcomp = x * Math.Cos(theta); double Ycomp = y * Math.Sin(theta);
                            int rp = (int)(Xcomp + Ycomp);
                            if (rp == rho && xstart == -1)
                            {
                                xstart = x;
                                ystart = y;
                                xend = x;
                                yend = y;
                            }
                            else if (rp == rho && xstart > -1)
                            {
                                if ((xend + maxGap) > x && (xend - maxGap) < x 
                                    /* && (xend + minGap) < x && (xend - minGap) > x 
                                    && (yend + minGap) < y && (yend - minGap) > y */
                                    && (yend + maxGap) > y && (yend - maxGap) < y)
                                {
                                    xend = x;
                                    yend = y;
                                }
                                else
                                {
                                    double length1 = Math.Sqrt(((xend - xstart) * (xend - xstart)) + ((yend - ystart) * (yend - ystart)));
                                    if (length1 > minLength)
                                    {
                                        Lines.Add(new Tuple<int, int, int, int>(xstart, xend, ystart, yend));
                                    }
                                    xstart = xend = x;
                                    ystart = yend = y;
                                }
                            }
                        }
                    }
                double length = Math.Sqrt(((xend - xstart) * (xend - xstart)) + ((yend - ystart) * (yend - ystart)));
                if (length > minLength)
                {
                    Lines.Add(new Tuple<int, int, int, int>(xstart, xend, ystart, yend));
                }
            }

            return Lines;
        }

        private int[,] normalizePeaks(byte[,] inputImage, int[,] array)
        {
            byte[,] testImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];
            for (int xt = 0; xt < testImage.GetLength(0); xt++)
                for (int yt = 0; yt < testImage.GetLength(1); yt++)
                    testImage[xt, yt] = 255;
            int[,] normalizedarray = array;
            for (int i = 0; i < array.Length; i++)
            {
                int theta = array[i, 0];
                int rho = array[i, 1];
                int maxHits = 0;
                for (int x = 0; x < inputImage.GetLength(0); x++)
                    for (int y = 0; y < inputImage.GetLength(1); y++)
                    {
                        if (testImage[x, y] > 255)
                        {
                            double rp = x * Math.Cos(theta) + y * Math.Sin(theta);
                            if (rp == rho)
                                maxHits++;
                        }
                    }
                normalizedarray[theta, rho] = array[theta, rho] / maxHits;
            }

            return normalizedarray;
        }

        //Draws the lines found in lineDectection
        private byte[,] visualizeHoughLineSegments(byte[,] edgeImage, List<Tuple<int, int, int, int>> Lines)
        {
            byte[,] outputImage = edgeImage;
            // [,] outputImage = new byte[edgeImage.GetLength(0), edgeImage.GetLength(1)];
            for (int x = 0; x < outputImage.GetLength(0); x++)
                for (int y = 0; y < outputImage.GetLength(1); y++)
                    outputImage[x, y] = 0;
            for (int i = 0; i < Lines.Count; i++)
            {
                int xstart = Lines[i].Item1;
                int xend = Lines[i].Item2;
                int ystart = Lines[i].Item3;
                int yend = Lines[i].Item4;
                if (xend - xstart == 0)
                {
                    while (ystart <= yend)
                    {
                        outputImage[xstart, ystart] = 255;
                        ystart += 1;
                    }
                }
                else
                {
                    double deltax = xend - xstart;
                    double deltay = yend - ystart;
                    double slope = deltay / deltax;
                    double yuse = ystart;
                    while (xstart <= xend)
                    {
                        outputImage[xstart, ystart] = 255;
                        xstart += 1;
                        yuse += slope;
                        ystart = (int)Math.Round(yuse);
                    }
                }
            }

            return outputImage;
        }

        private int[,] houghTransformAngleLimits(byte[,] inputImage, double lowerLimit, double upperLimit)
        {
            int maxR = (int)Math.Sqrt((inputImage.GetLength(0) * inputImage.GetLength(0))
                       + (inputImage.GetLength(1) * inputImage.GetLength(1))); //The max value r can be
            int rangeR = 2 * maxR; //r can be positive and negative
            int[,] transform = new int[inputImage.GetLength(0), rangeR];
            while (upperLimit >= 2 * Math.PI)
            {
                upperLimit -= 2 * Math.PI; //removes possible rotations by keeping the angle smaller than 360/2PI
            }
            while (lowerLimit >= 2 * Math.PI)
            {
                lowerLimit -= 2 * Math.PI; //removes possible rotations by keeping the angle smaller than 360/2PI
            }
            if (lowerLimit > upperLimit)
            {
                double store = lowerLimit;
                lowerLimit = upperLimit;
                upperLimit = store;
            }

            for (int x = 0; x < inputImage.GetLength(0); x++)
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    if (inputImage[x, y] == 1 || inputImage[x, y] == 255) //both "binary forms" arre accepted
                    {
                        //graph the hough transform per pixel
                        double thetaIncrement = 2 * Math.PI / inputImage.GetLength(0); //make theta easily drawable in an image
                        for (double theta = lowerLimit; theta <= upperLimit; theta += thetaIncrement) // change start and end of for loop to use lower and upper limit
                        {
                            //calculate r for every theta
                            double xPart = x * Math.Cos(theta);
                            double yPart = y * Math.Sin(theta);
                            double r = xPart + yPart;
                            int INTtheta = (int)((inputImage.GetLength(0) * theta) / Math.PI);
                            int INTr = (int)r + maxR;
                            transform[INTtheta, INTr] += 1; //Vote for the found value ///TODO: aanpassen?

                            //Also vote for the values around it
                            ///TODO: good values? good range around it?
                            ///TODO: change to use double and move the ints to draw
                            for (int dtheta = -1; dtheta <= 1; dtheta++)
                            {
                                for (int dr = -1; dr <= 1; dr++)
                                {
                                    INTtheta += dtheta;
                                    INTr += dr;
                                    if (INTr > 0 && INTtheta > 0
                                        && INTr < transform.GetLength(1) && INTtheta < transform.GetLength(0))
                                        transform[INTtheta, INTr] += 1; //The centre will be voted for stronger
                                }
                            }
                        }
                    }
                    else if (inputImage[x, y] > 1 && inputImage[x, y] < 255)
                    {
                        inputImage = makeBinaryImage(thresholdImage(inputImage, 23));
                    }
                }
            }

            //accumulator array: make it image size so it can be displayed

            return transform;
        }

        private List<Tuple<int, int>> detectCrossings(byte[,] inputImage, List<double[]> peaks, int threshold, int maxGap, int minLength)
        {
            List<double[]> peakswithcrossing = new List<double[]>();
            List<Tuple<int, int>> crossingcoordinates = new List<Tuple<int, int>>();
            for (int i = 0; i < peaks.Count; i++)
            {
                double theta1 = peaks[i][0];
                double rho1 = peaks[i][1];
                double cos1 = Math.Cos(theta1);
                double sin1 = Math.Sin(theta1);
                for (int n = i + 1; n < peaks.Count; n++)
                {
                    double theta2 = peaks[n][0];
                    double rho2 = peaks[n][1];
                    double cos2 = Math.Cos(theta2);
                    double sin2 = Math.Sin(theta2);

                    double a = (cos2 * sin1) - (cos1 * sin2);
                    double b = rho2 * sin1 - rho1 * sin2;
                    int x = (int)(b / a);
                    int y1 = (int)((rho1 - cos1 * x) / sin1);
                    int y2 = (int)((rho2 - cos2 * x) / sin2);
                    if (y1 == y2)
                    {
                        if (inputImage[x, y1] > 0)
                        {
                            crossingcoordinates.Add(new Tuple<int, int>(x, y1));
                        }

                    }
                }
            }

            return crossingcoordinates;
        }

        private byte[,] visualizeCrossingLines(byte[,] inputImage, List<double[]> peaks, int threshold, int maxGap, int minLength)
        {
            List<double[]> peakswithcrossing = new List<double[]>();
            for (int i = 0; i < peaks.Count; i++)
            {
                double theta1 = peaks[i][0];
                double rho1 = peaks[i][1];
                double cos1 = Math.Cos(theta1);
                double sin1 = Math.Sin(theta1);
                for (int n = i + 1; n < peaks.Count; n++)
                {
                    double theta2 = peaks[n][0];
                    double rho2 = peaks[n][1];
                    double cos2 = Math.Cos(theta2);
                    double sin2 = Math.Sin(theta2);

                    double a = (cos2 * sin1) - (cos1 * sin2);
                    double b = rho2 * sin1 - rho1 * sin2;
                    int x = (int)(b / a);
                    int y1 = (int)((rho1 - cos1 * x) / sin1);
                    int y2 = (int)((rho2 - cos2 * x) / sin2);
                    if (y1 == y2)
                    {
                        if (inputImage[x, y1] > 0)
                        {
                            double[] line1 = peaks[i];
                            double[] line2 = peaks[n];
                            peakswithcrossing.Add(line1);
                            peakswithcrossing.Add(line2);
                        }

                    }
                }
            }

            List<Tuple<int, int, int, int>> CrossingLines = houghlineDetection(inputImage, peakswithcrossing, threshold, minLength, maxGap);
            byte[,] outputImage = visualizeHoughLineSegments(inputImage, CrossingLines);
            return outputImage;
        }

        byte[,] visualizeCrossingpoints(byte[,] inputImage, List<Tuple<int,int>> crossings)
        {
            byte[,] outputImage = inputImage;
            for (int x = 0; x < outputImage.GetLength(0); x++)
                for (int y = 0; y < outputImage.GetLength(1); y++)
                    outputImage[x, y] = 0;

            for(int i = 0; i < crossings.Count; i++)
            {
                int xc = crossings[i].Item1;
                int yc = crossings[i].Item2;
                outputImage[xc, yc] = 255;
            }

            return outputImage;
        }

        
        //Creates an AccumulatorArray for circles within the bounds
        private int[,] houghTransformCircle(byte[,] inputImage, double lowerR, double upperR)
        {
            int maxR = (int)Math.Sqrt((inputImage.GetLength(0) * inputImage.GetLength(0))
                       + (inputImage.GetLength(1) * inputImage.GetLength(1))); //The max value r can be
            int rangeR = 2 * maxR; //r can be positive and negative
            int[,] transform = new int[inputImage.GetLength(0), rangeR];
            double lowerR2 = lowerR * lowerR;
            double upperR2 = upperR * upperR;

            for (int x = 0; x < inputImage.GetLength(0); x++)
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    //Check all "on" pixels to see if they could be part of a circle
                    if (inputImage[x, y] == 1 || inputImage[x, y] == 255) //both "binary forms" arre accepted
                    {
                        //go over all possible circles to see if they would work
                        for (int a = 0; a < transform.GetLength(0); a++)
                        {
                            for (int b = 0; b < transform.GetLength(1); b++)
                            {
                                double xComp = Math.Pow((a + x), 2);
                                double yComp = Math.Pow((b + y), 2);
                                if (xComp + yComp >= lowerR2 && xComp + yComp <= upperR2)
                                {
                                    transform[a, b]++;
                                }
                            }
                        }
                    }
                }
            }

            return transform;
        }

        //////
        ///TEST FOR ASSIGNMENT 4
        //////
        
        private void tUp (object sender, System.EventArgs e)
        {
            /*if (Threshold <= 5) down = false;
            if (Threshold >= 14) down = true;
            if (Threshold > 4 && down) Threshold--;
            if (Threshold < 15 && !down) Threshold++;*/
            Console.WriteLine(Threshold);
        }

        private byte[,] noiseSuppressor(byte[,] inputImage, int kernelSize)
        {
            //float[,] noiseKernel = new float[,] { { 1, 1, 1 }, { 1, -8, 1 }, { 1, 1, 1 } };
            //byte[,] output = convolveImage(inputImage, noiseKernel);
            ///TODO: ^Change
            byte[,] output = inputImage;

            for (int x = 0; x < inputImage.GetLength(0); x++)
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    //Per pixel
                    int ayeVotes = 0; int nayVotes = 0; //Vote wether to keep the current value
                    int kernelLimit = (int)(0.5 * kernelSize); //Spread of the voting
                    for (int dI = -kernelLimit; dI <= kernelLimit; dI++)
                    {
                        for (int dJ = -kernelLimit; dJ <= kernelLimit; dJ++)
                        {
                            //Per kernel entry
                            int nX = x + dI; int nY = y + dJ;
                            if (!(nX == x && nY == y) 
                                && nX >= 0 && nX < inputImage.GetLength(0) 
                                && nY >= 0 && nY < inputImage.GetLength(1))
                            {
                                if (inputImage[nX, nY] == inputImage[x, y]) ayeVotes++;
                                else nayVotes++;
                            }
                        }
                    }

                    if (inputImage[x, y] == 0)
                    {
                        if (nayVotes > ayeVotes) ///TODO: Put in the >= ? 
                        {
                            output[x, y] = 255; ///TODO: Or 1?
                        }
                    }
                    else
                    {
                        if (nayVotes > ayeVotes)
                        {
                            output[x, y] = 0;
                        }
                    }
                }
            }


            return output;
        }

        private byte[,] borderDeletion(byte[,] inputImage, double borderPercentage)
        {
            byte[,] output = inputImage;           
            for (int x = 0; x < inputImage.GetLength(0); x++)
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    if (x < borderPercentage * inputImage.GetLength(0) || x > (1-borderPercentage) * inputImage.GetLength(0) 
                        || y < borderPercentage * inputImage.GetLength(1) || y > (1 - borderPercentage) * inputImage.GetLength(1))
                    {
                        output[x, y] = 0;
                    }
                }
            }

            MinX = (int)(borderPercentage * inputImage.GetLength(0)) - 1;
            MaxX = (int)((1 - borderPercentage) * inputImage.GetLength(0)) - 1;
            MinY = (int)(borderPercentage * inputImage.GetLength(1)) - 1;
            MaxY = (int)((1 - borderPercentage) * inputImage.GetLength(1)) - 1;

            return output;
        }

        private byte[,] lungEdgesDetector(byte[,] inputImage)
        {
            byte[,] output = inputImage;
            bool xFound = false; bool secondXFound = false;
            int firstX = 0; int firstY = 0;
            int leftFirstX = 0; int leftFirstY = 0;  

            while (!xFound)
            {
                int y = (int)(inputImage.GetLength(1) * 0.5);
                int startY = y;
                for (int x = 0; x < 0.5 * inputImage.GetLength(0) && !xFound; x++)
                {
                    if (inputImage[x, y] == 1 || inputImage[x, y] == 255)
                    {
                        firstY = y;
                        firstX = x;
                        xFound = true;
                    }
                }
                if (y <= startY + 10 && y >= startY) y++;
                else if (y > startY) y = startY -1;
                if (y >= startY - 10 && y < startY) y--;
                else if (y < startY - 10) break;
            }
            while (!secondXFound)
            {
                int y = (int)(inputImage.GetLength(1) * 0.5);
                int startY = y;
                for (int x = inputImage.GetLength(0) - 1; x > 0.5 * inputImage.GetLength(0) && !secondXFound; x--)
                {
                    if (inputImage[x, y] == 1 || inputImage[x, y] == 255)
                    {
                        leftFirstY = y;
                        leftFirstX = x;
                        xFound = true;
                    }
                }
                if (y <= startY + 10 && y >= startY) y++;
                else if (y > startY) y = startY - 1;
                if (y >= startY - 10 && y < startY) y--;
                else if (y < startY - 10) break;
            }

            //Go over all the pixels of that side until the clavicula is met
            //set clavicula pixels to a certain value
            bool claviculaFound = false; bool otherClavFound = false;
            bool diaphramFound = false; bool otherDiaFound = false;
            int xCurrent = firstX; int yCurrent = firstY;
            bool useFirstLeft = false; bool resetRight = false; bool resetLeft = false;
            while (!claviculaFound && xFound && !otherClavFound) ///TODO: otherside and moving up
            {
                int[] nextPixel = new int[2];
                if (!claviculaFound) nextPixel = nextPixelFinder(inputImage, xCurrent, yCurrent, true, true); ///TODO: other side?
                else if (!diaphramFound)
                {
                    //Goes to find the right diaphram
                    if (resetRight)
                    {
                        xCurrent = firstX; yCurrent = firstY;
                        resetRight = false;
                    }
                    nextPixel = nextPixelFinder(inputImage, xCurrent, yCurrent, true, false);
                }
                else if (!otherClavFound)
                {
                    //Finds left clavicula
                    if (useFirstLeft)
                    {
                        xCurrent = leftFirstX; yCurrent = leftFirstY;
                        useFirstLeft = false;
                    }
                    nextPixel = nextPixelFinder(inputImage, xCurrent, yCurrent, false, true);
                }
                else if(!otherDiaFound)
                {
                    //Finds left diaphram
                    if (resetLeft)
                    {
                        xCurrent = leftFirstX; yCurrent = leftFirstY;
                        resetLeft = false;
                    }
                    nextPixel = nextPixelFinder(inputImage, xCurrent, yCurrent, false, false);
                }
                if (nextPixel[0] == 0 && nextPixel[1] == 0) //in case no output was found
                {
                    MessageBox.Show("No lung was found");
                    return output;
                }

                //Fill in the gaps
                Point currentPoint = new Point();
                currentPoint.X = xCurrent; currentPoint.Y = yCurrent;
                Point nextPoint = new Point();
                nextPoint.X = nextPixel[0]; nextPoint.Y = nextPixel[1];
                Point highest = new Point(); Point lowest = new Point();
                if(nextPoint.Y >= currentPoint.Y)
                {
                    highest = nextPoint;
                    lowest = currentPoint;
                }
                else
                {
                    highest = currentPoint;
                    lowest = nextPoint;
                }
                output = fillInLungs(inputImage, lowest, highest);

                //Check when above clavicula or the border was reached
                //When going up
                xCurrent = nextPoint.X; yCurrent = nextPoint.Y;
                if (!claviculaFound)
                {
                    float a = caviculaLines[0]; float b = caviculaLines[1]; ///TODO: change for left side
                    float yForThisX = a * xCurrent + b;
                    if (yCurrent >= yForThisX)
                    {
                        claviculaFound = true;
                        resetRight = true;
                    }
                }
                else if (!diaphramFound)
                {
                    ///TODO: diafragma in zetten
                    
                }
                else if (!otherClavFound)
                {
                    float a = caviculaLines[2]; float b = caviculaLines[3]; ///TODO: change for left side
                    float yForThisX = a * xCurrent + b;
                    if (yCurrent >= yForThisX)
                    {
                        otherClavFound = true;
                        resetLeft = true;
                    }
                }
                else if (!otherDiaFound)
                {
                    ///TODO: voor andere diafragma testen
                }
                //End of while
            }
            ///TODO: mark pixels part of the lung edge
            return output;
        }

        private int[] nextPixelFinder(byte[,] image, int currentX, int currentY, bool rightSide, bool up) //please not that the right side of an x-ray is the left side of the image
        {
            int[] output = new int[2];
            sbyte dX; sbyte dY;
            bool borderReached = false; int reachedY = 0;
            sbyte vertDir = -1; //-1 is up in an image
            if(!up) vertDir = 1;

            List<sbyte[]> viewerCone = getViewerCone(rightSide);
            foreach (sbyte[] entry in viewerCone)
            {
                dX = entry[0]; dY = (sbyte)(entry[1] * vertDir); //-1 to go up
                if (dX == 0 && dY == 0) continue; //safety in case it accidentally looks at the current pixel
                int stepX = currentX + dX; int stepY = currentY + dY;
                if (image[stepX, stepY] >= 0)
                {
                    output[0] = stepX; output[1] = stepY;
                    return output;
                }
                if (stepY <= MinY)
                {
                    borderReached = true;
                    reachedY = stepY;
                }
                else if (stepY >= MaxY)
                {
                    borderReached = true;
                    reachedY = stepY; ///TODO: when lower border hit no diaphram was found
                }
            }
            if (borderReached)
            {
                while (reachedY >= MaxY && reachedY <= MinY) reachedY -= vertDir;
                output[0] = currentX; output[1] = reachedY; //Return the first non-deleted pixel
                if (up)
                {
                    if(rightSide)
                    {
                        rightUpperBorder.X = currentX; rightUpperBorder.Y = currentY;
                    }
                    else
                    {
                        leftUpperBorder.X = currentX; leftUpperBorder.Y = currentY;
                    }
                }
                else
                {
                    if (rightSide)
                    {
                        rightLowerBorder.X = currentX; rightLowerBorder.Y = currentY;
                    }
                    else
                    {
                        leftLowerBorder.X = currentX; leftLowerBorder.Y = currentY;
                    }
                }
            }

            return output;
        }

        private List<sbyte[]> getViewerCone (bool rightSide)
        {
            List<sbyte[]> list = new List<sbyte[]>();
            sbyte[] currentAddition = new sbyte[2];
            sbyte sideDet = 1;
            if (!rightSide) sideDet = -1;

            sbyte dX = sideDet; sbyte dY = 1;
            for (; dY <= 8; dY++)
            {
                currentAddition[0] = 0;
                currentAddition[1] = dY;
                list.Add(currentAddition);
            }
            for (; dX * sideDet < 3; dX+=sideDet)
            {
                for(dY = 1; dY <= 8; dY++)
                {
                    currentAddition[0] = dX;
                    currentAddition[1] = dY;
                    list.Add(currentAddition);
                }
            }
            for (dX = (sbyte)(sideDet*-1); dX * sideDet > -3; dX-= sideDet)
            {
                for (dY = 1; dY <= 8; dY++)
                {
                    currentAddition[0] = dX;
                    currentAddition[1] = dY;
                    list.Add(currentAddition);
                }
            }
            //First section filled
            for (dY = 1 + 8; dY <= 8 + 7; dY++)
            {
                currentAddition[0] = 0;
                currentAddition[1] = dY;
                list.Add(currentAddition);
            }
            for (dX = sideDet; dX * sideDet < 5; dX+= sideDet)
            {
                for (dY = 1 + 8; dY <= 8 + 7; dY++)
                {
                    currentAddition[0] = dX;
                    currentAddition[1] = dY;
                    list.Add(currentAddition);
                }
            }
            for (dX = (sbyte)(sideDet * -1); dX * sideDet > -5; dX-= sideDet)
            {
                for (dY = 1 + 8; dY <= 8; dY++)
                {
                    currentAddition[0] = dX;
                    currentAddition[1] = dY;
                    list.Add(currentAddition);
                }
            }
            //Last section filled

            return list;
        }

        private byte[,] fillInLungs(byte[,] inputImage, Point lowestPoint, Point highestPoint)
        {
            byte[,] output = inputImage;

            int dX = highestPoint.X - lowestPoint.X;
            int dY = highestPoint.Y - lowestPoint.Y;
            float slope; string slopeType = "dxdy";
            if (dX == 1 || dX == -1 || dY == 1 || dY == -1) return output; //in case of already bordering pixels
            if (dX == 0 && dY == 0) return output; //in case it's the same pixel twice
            if (dX >= dY)
            {
                slope = dY / dX;
                slopeType = "dydx";
            }
            else  slope = dX / dY;

            int startX = lowestPoint.X; int startY = lowestPoint.Y;
            int x = 0; int y = 0;
            if(slopeType == "dydx")
            {
                int b = lowestPoint.Y - (int)(slope * lowestPoint.X);
                for (x = lowestPoint.X; x <= highestPoint.X; x++)
                {
                    y = (int)(slope * x) + b;
                    output[x, y] = 255; ///TODO: change to determine what was done?
                }
            }
            else
            {
                int b = lowestPoint.X - (int)(slope * lowestPoint.Y);
                for (y = lowestPoint.Y; y <= highestPoint.Y; y++)
                {
                    x = (int)(slope * y) + b;
                    output[x, y] = 255; ///TODO: see above
                }
            }

            ///TODO: maybe add an extra line between highest en lowest
            return output;
        }

        private double chestProbablity(byte[,] inputImage)
        {
            double p = 0;

            int chestWidth;
            int rightWidestX = inputImage.GetLength(0); int leftWidestX = 0;
            if (lungs == null) return p = 0.2;
            foreach (int[] coord in lungs)
            {
                if (coord[0] < rightWidestX) rightWidestX = coord[0];
                else if (coord[0] > leftWidestX) leftWidestX = coord[0];
            }
            chestWidth = rightWidestX - leftWidestX;
            double lungToImageRatio = chestWidth / inputImage.GetLength(0);
            bool claviculaeFound = true;
            bool diaphramFound = true; ///TODO: how to find it
            Point emptyPoint = new Point(0,0);
            if (leftUpperBorder == emptyPoint || rightUpperBorder == emptyPoint) claviculaeFound = false;

            if (claviculaeFound && diaphramFound)
            {
                //expected ratio is 0.8
                if (lungToImageRatio > 0.7 && lungToImageRatio < 0.85) return p = 0.95;
                if (lungToImageRatio >= 0.85) p = 0.75 + (0.85 - lungToImageRatio); ///TODO: fine-tuning
                else if (lungToImageRatio <= 0.7) p = lungToImageRatio;
            }
            else if (!claviculaeFound && diaphramFound)
            {
                if (lungToImageRatio >= 0.85) p = 0.7 + (0.85 - lungToImageRatio);
                else if (lungToImageRatio <= 0.7) p = lungToImageRatio;
                else p = 0.85;
            }
            else if (claviculaeFound && !diaphramFound)
            {
                //if no diaphram is found it is propably not a x-thorax, or of such bad quality a heart will also not be found
                p = 0.2;
            }
            else if (!claviculaeFound && !diaphramFound)
            {
                p = 0.588 * lungToImageRatio; //If really zoomed in the chance is higher
            }
            ///TODO: enough done?

            return p;
        }

        private void MAX(byte[,] inputImage)
        {
            int maxValue = 0;
            for (int x = 0; x < inputImage.GetLength(0); x++)
            {
                for (int y = 0; y < inputImage.GetLength(1); y++)
                {
                    if (inputImage[x,y] > maxValue) maxValue = inputImage[x,y];
                    if (inputImage[x, y] > 230) Console.WriteLine(inputImage[x, y]);
                }
            }
            Console.WriteLine("Max is " + maxValue);
        }

        ///TODO: make extra lung filler for all lung that is already white
    }
}