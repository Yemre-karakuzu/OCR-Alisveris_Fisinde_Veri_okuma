using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using System.IO;
using net.zemberek.erisim;
using net.zemberek.tr.yapi;
using System.Text.RegularExpressions;
using Microsoft;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Proje1
{
    public partial class Form1 : Form
    {
        public Tesseract.Rect bounds; //segmentlerin görsel olarak gösterimi için çizilecek olan dikdörtgenin bilgileri
        public Form1()
        {
            InitializeComponent();
        }

        private static string GetNumbers(string input)//gönderilen satırdaki sayilari geri döndürür
        {
            return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }
        public float DigitOnly(string line)//toplam, tam sayı olsa bile virgül ile ifade edildiği için toplam satırındaki float değerini elde etme fonksiyonu
        {
            string sumLine = "";
            float toplam = -1;
            try
            {
                if (line.Contains(","))
                {
                    string[] x = line.Split(',');
                    sumLine += GetNumbers(x[0]);
                    sumLine += ".";
                    sumLine += GetNumbers(x[1]);
                    toplam = float.Parse(sumLine);
                }
                else
                {
                    string[] x = line.Split('.');
                    sumLine += GetNumbers(x[0]);
                    sumLine += ".";
                    sumLine += GetNumbers(x[1]);
                    toplam = float.Parse(sumLine);
                }
            }
            catch
            {

            }
            
            return toplam;
        }
        public static Bitmap MakeGrayscale3(Bitmap original) // görseli grayscale hale dönüştürme fonksiyonu
        {           
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);
           
            using (Graphics g = Graphics.FromImage(newBitmap))
            {                
                ColorMatrix colorMatrix = new ColorMatrix(
                   new float[][]
                   {
             new float[] {.3f, .3f, .3f, 0, 0},
             new float[] {.59f, .59f, .59f, 0, 0},
             new float[] {.11f, .11f, .11f, 0, 0},
             new float[] {0, 0, 0, 1, 0},
             new float[] {0, 0, 0, 0, 1}
                   });
               
                using (ImageAttributes attributes = new ImageAttributes())
                {
                   
                    attributes.SetColorMatrix(colorMatrix);
                  
                    g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                                0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
                }
            }
            return newBitmap;
        }

        private void Form1_Load(object sender, EventArgs e)
        {          
            
        }

        private void button1_Click(object sender, EventArgs e) //dosya yaani görselin seçilip işleneceği button fonksiyonu
        {
            //tekrar kullanım için temizleme 
            listView1.Items.Clear(); 
            listView1.BackColor = Color.White;
            string path = "";

            //dosyanın seçim ekranı
            OpenFileDialog dosya = new OpenFileDialog();

            if (dosya.ShowDialog() == DialogResult.OK)
            {
                path = dosya.FileName;
            }

            var img = new Bitmap(path);
            img = MakeGrayscale3(img);
            var ocr = new TesseractEngine("./tessdata", "eng"); // ocr ın tanımlanması

            pictureBox1.Image = img;
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

            var output = ocr.Process(img); //görselin string haline dönüştürülmesi
            string text = output.GetText();

            File.WriteAllText("output.txt", text); //elde edilen stringin txt e yazılması


            var zemberek = new Zemberek(new TurkiyeTurkcesi()); //Nlp

            pictureBox1.Image = img;

            listView1.View = View.Details;
            listView1.GridLines = true;
            listView1.FullRowSelect = true;

            listView1.Columns.Add("Urun", 200);
            listView1.Columns.Add("Kdv", 100);
            listView1.Columns.Add("Fiyat", 80);

            string tarih;
            string saat;          
            float toplam = 0;
            float toplamKontrol = 0;           
            bool t = true;
            string[] line = File.ReadLines("output.txt").ToArray();

            //regex 
            string pattern = @"(.+)+(%[0-9] +)+(.+)";           
            string datePattern = @"(.*)([0-9][0-9])[\/]?.([0-9][0-9])[\/]?.(\d{4})(.*)";
            string timePattern = @"([0-2]?[0-9])[:]([0-5][0-9])";
            Regex re = new Regex(pattern, RegexOptions.Compiled);
            Regex Dre = new Regex(datePattern, RegexOptions.Compiled);
            Regex Tre = new Regex(timePattern, RegexOptions.Compiled);

            //segmentlerin tanımlanması ve başlıklandırılması 
            int c = 1;
            using (var iter = output.GetIterator())
            {
                iter.Begin();
                do
                {
                    if (!String.IsNullOrWhiteSpace(iter.GetText(PageIteratorLevel.Block)))
                    {
                        string segmentLabel = "";
                        if (c == 1)
                        {
                            segmentLabel += "Market Bilgileri";
                        }
                        string segmentText = iter.GetText(PageIteratorLevel.Block).ToString();
                        string[] segmentWords = segmentText.Split(' ');

                        if (t && segmentText.Contains("%") && !segmentText.Contains("#"))
                        {

                            if (!segmentLabel.Contains("Urun Ve Fiyat Bilgileri"))
                            {
                                segmentLabel += "Urun Bilgileri" + Environment.NewLine;
                            }

                        }
                        foreach (string word in segmentWords)
                        {
                            if (t && word.Any(char.IsDigit))
                            {
                                foreach (string s in zemberek.oner(word))
                                {

                                    if (s == "toplam" || s == "Toplam" || s == "TOPLAM")
                                    {
                                        segmentLabel += "Fiyat Ve Toplam Bilgileri" + Environment.NewLine;
                                        t = false;

                                    }
                                    if (s == "tarih" || s == "Tarih" || s == "TARİH" || s == "TARIH" || s == "saat" || s == "Saat" || s == "SAAT")
                                    {
                                        Match m = Dre.Match(segmentText);
                                        if (m.Success)
                                        {
                                            if (!segmentLabel.Contains("Tarih ve Saat Bilgileri"))
                                            {
                                                segmentLabel += "Tarih ve Saat Bilgileri" + Environment.NewLine;
                                            }
                                        }
                                    }

                                }
                            }
                        }

                        //segment konum bilgilerinin kullanıcıya gösteriminin yapılması
                        var blockType = iter.BlockType;
                        Rect bounds = Rect.Empty;
                        iter.TryGetBoundingBox(PageIteratorLevel.Block, out bounds);

                        Pen pen = new Pen(Color.Red, 2);

                        Graphics g = Graphics.FromImage(img);
                        g.DrawRectangle(pen, bounds.X1, bounds.Y1, bounds.Width, bounds.Height);
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        g.DrawString(segmentLabel, new Font("Tahoma", 16), Brushes.Red, bounds.X1 + bounds.Width - 180, bounds.Y1);

                        c++;
                    }

                } while (iter.Next(PageIteratorLevel.Block));

            }

            //ocr dan elde edilen metin bilgisinde satır satır ilerleyip kural tabanını uygulama kısmı
            t = true;
            string linePrev = "";
            for (int l = 0; l < line.Length; l++)
            {
                string[] lineWords = line[l].Split(' ');

                for (int i = 0; i < lineWords.Length; i++) // Her bir sözcüğün teker teker kontrol edilmesi
                {
                    if (t && line[l].Any(char.IsDigit))
                    {
                        foreach (string s in zemberek.oner(lineWords[i])) //Nlp önerisi ve doğrulama
                        {                           
                            if (s == "toplam" || s == "Toplam" || s == "TOPLAM")
                            {
                                toplam = DigitOnly(line[l]);
                                label5.Text = toplam.ToString();
                                Console.WriteLine("Toplam: " + toplam);
                                t = false;
                                break;
                            }
                            if (s == "tarih" || s == "Tarih" || s == "TARİH" || s == "TARIH")
                            {
                                label1.Text = s;
                                tarih = lineWords[i];
                                label1.Text = tarih;
                                Match m = Dre.Match(line[l]);
                                if (m.Success)
                                {                                                                      
                                    tarih = m.Groups[2].ToString() + "/" + m.Groups[3].ToString() + "/" + m.Groups[4].ToString();
                                    label1.Text = tarih;
                                }
                            }
                            if (s == "saat" || s == "Saat" || s == "SAAT")
                            {
                                saat = lineWords[i];
                                label2.Text = saat;
                                Match m = Tre.Match(line[l]);
                                if (m.Success)
                                {                                                                     
                                    saat = m.Groups[1].ToString() + ":" + m.Groups[2].ToString();
                                    label2.Text = saat;
                                }
                            }
                        }
                    }
                }

                if (t && line[l].Contains("%"))//satırın % KDV içeriğ içermediğinin kontrolü ve regexl işlemine alınması
                {                    
                    Match m = re.Match(line[l]);
                    if (m.Success)
                    {                        
                        string miktar = "";
                        Match a = re.Match(linePrev);
                        if (!a.Success)
                        {
                            if (linePrev.Contains("AD") || linePrev.Contains("ADET") || linePrev.Contains("KG") || linePrev.Contains("GR"))
                            {
                                miktar = linePrev;
                            }
                        }                       
                        float fiyat = DigitOnly(m.Groups[3].ToString());
                        string[] row = { miktar + " " + m.Groups[1].ToString(), m.Groups[2].ToString(), fiyat.ToString() };
                        var satir = new ListViewItem(row);
                        listView1.Items.Add(satir);
                        toplamKontrol += fiyat;                       
                    }
                }
                if (!String.IsNullOrWhiteSpace(line[l]))
                {
                    linePrev = line[l];
                }
            }
            if (toplam != toplamKontrol)//ürün fiyatlarının toplanıp toplam başlığı bilgisi ile karşılaştırılması
            {
                listView1.BackColor = Color.Orange;
            }

        }
    }
}

