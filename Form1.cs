using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace INFOIBVns
{
    public partial class INFOIBV : Form
    {
        private Bitmap InputImage;
        private Bitmap OutputImage;
        sbyte[,] Sobelx = new sbyte[,] { { -1, 0, 1 }, { -2, 0, 2 }, { 1, 0, 1 } }; // sobel kernels
        sbyte[,] Sobely = new sbyte[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };
        public INFOIBV()
        {
            InitializeComponent();
        }

        /*
         * loadButton_Click: process when user clicks "Load" button
         */
        private void loadImageButton_Click(object sender, EventArgs e)
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

        //Does Image B 
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
            workingImage = convolveImage(workingImage, gaussianKernel);
            workingImage = edgeMagnitude(workingImage, Sobelx, Sobely);
            workingImage = thresholdImage(workingImage);

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

        //Does image C
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
            workingImage = thresholdImage(workingImage);
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

        //All functions can easily be tested here
        private void applyButtonTest_Click(object sender, EventArgs e)
        {
            if (InputImage == null) return;                                 // get out if no input image
            if (OutputImage != null) OutputImage.Dispose();                 // reset output image
            OutputImage = new Bitmap(InputImage.Size.Width, InputImage.Size.Height); // create new output image
            Color[,] Image = new Color[InputImage.Size.Width, InputImage.Size.Height]; // create array to speed-up operations (Bitmap functions are very slow)
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                    Image[x, y] = InputImage.GetPixel(x, y);                // set pixel color in array at (x,y)

            ///TODO: Here code can be tested
            //
            //To test the functions simply remove the "//" for the desired function
            //Concatenations not in the assignment can be made manually
            //

            //Uncomment these to test
            byte[,] workingImage = convertToGrayscale(Image);          // convert image to grayscale
            //workingImage = invertImage(workingImage);         // inverts the image
            //workingImage = adjustContrast(workingImage);      // Adjusts contrast
            float[,] gaussianKernel = createGaussianFilter(5, 1);      // Create filter kernel
            //workingImage = convolveImage(workingImage, gaussianKernel); //Adds gaussian blur
            //workingImage = medianFilter(workingImage, 3);       // Adds a median filter
            //workingImage = edgeMagnitude(workingImage, Sobelx, Sobely); //Gives the magnitude of edges
            //workingImage = thresholdImage(workingImage);         // Thresholds image

            /* D1-D5
            float[,] gaussianKernel3 = createGaussianFilter(3, 1);
            float[,] gaussianKernel5 = createGaussianFilter(5, 3);
            float[,] gaussianKernel7 = createGaussianFilter(7, 5);
            float[,] gaussianKernel9 = createGaussianFilter(9, 7);
            float[,] gaussianKernel11 = createGaussianFilter(11, 9);
            byte[,] gaussian = thresholdImage(edgeMagnitude(convolveImage(workingImage, gaussianKernel11), sobelx, sobely)); */

            //Concatenations testing area
            workingImage = convolveImage(thresholdImage(adjustContrast(workingImage)), gaussianKernel); //Output

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
                    byte invert = (byte)(maxValue - pixelvalue);            //Inverts
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
            float aMin = (lowestValue * q); float aMax = (highestValue * (1-q)); //apply the CI
            if (aMin == 0 && aMax == 255)
            {
                MessageBox.Show("Your image has maximum contrast");
                return inputImage;
            } 
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
                    filter[tX,tY] = (float)Math.Exp(exponentPart);
                    sumComponents += filter[tX, tY];
                }
            }

            float[,] result = new float[size, size];
            for (byte cX = 0; cX < size; cX++) //loop over all components in the filter
            {
                for (byte cY = 0; cY < size; cY++)
                {
                    result[cX, cY] = filter[cX,cY] / sumComponents; //Normalise the kernel
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
            // TODO: add your functionality and checks, think about border handling and type conversion

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
                            if ((tX + dX  >= 0 && tY + dY >= 0) && (tX + dX < InputImage.Size.Width && tY + dY < InputImage.Size.Height))
                            {
                                int newX = tX + dX; int newY = tY + dY;
                                valueArray += refImage[newX, newY] * filter[tFX, tFY];
                            }
                            else
                            {
                                valueArray += 123 * filter[tFX, tFY]; //Handles borders
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



            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                {
                    int Dx = 0;
                    int Dy = 0;
                    for (int tFX = -1; tFX <= 1; tFX++)
                        for (int tFY = -1; tFY < 1; tFY++)
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
        private byte[,] thresholdImage(byte[,] inputImage)
        {
            // create temporary grayscale image
            byte[,] tempImage = new byte[inputImage.GetLength(0), inputImage.GetLength(1)];

            // TODO: add your functionality and checks, think about how to represent the binary values
            for (int x = 0; x < InputImage.Size.Width; x++)                 // loop over columns
            {
                for (int y = 0; y < InputImage.Size.Height; y++)            // loop over rows
                {
                    byte pixelvalue = inputImage[x, y];
                    if (pixelvalue < 23) tempImage[x, y] = 0;               //Check against the threshold
                    else tempImage[x, y] = 255;
                }
            }
            return tempImage;
        }

        
        // ====================================================================
        // ============= YOUR FUNCTIONS FOR ASSIGNMENT 2 GO HERE ==============
        // ====================================================================


        // ====================================================================
        // ============= YOUR FUNCTIONS FOR ASSIGNMENT 3 GO HERE ==============
        // ====================================================================

    }
}