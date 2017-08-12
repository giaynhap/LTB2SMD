using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Globalization;
using System.Diagnostics;
using SevenZip;
using FreeImageAPI;
using ManagedSquish;
using System.Reflection;
namespace LTB_SMD_GN
{
    public struct MeshData
    {
    
        public string name;
        public uint nvertices;
        public uint nIdx;
        public List<float[]> vertices;
        public List<float[]> normals;
        public List<float[]> uvs;
        public List<float[]> weights;
        public List<int[]> weightsets;
        public List<int> weightsets_output;
        public List<int> triangles;
        public uint type;
    };
    public struct BoneData
    {
        public string name;
        public uint nSubbone;
        public double[,] matdata;
        public string bone_data_out;
        public uint isbone;
        public uint num2;
        public int par;
    };
    public struct framedata
    {
        int indexframe;
        public List<float[]> pos;
        public List<float[]> quats;

    }
    public struct AnimData
    {
        public string name;
        public uint nkeyframe;
        public List<int> listkeyframe;
        public List<string> listsound;
        public float[] Dim;
        public int interp_time;
        public framedata[] frame;

    };
    public partial class Form1 : Form
    {
        public const int LTB_MESHTYPE_NOTSKINNED = 1;
        public const int LTB_MESHTYPE_EXTRAFLOAT = 2;
        public const int LTB_MESHTYPE_SKINNED = 4;
        public const int LTB_MESHTYPE_SKINNEDALT = 3;
        public const int LTB_MESHTYPE_TWOEXTRAFLOAT = 5;

        public const float NKF_TRANS_SCALE_1_11_4 = (16.0f);		// 2^4
        public const float NKF_TRANS_OOSCALE_1_11_4 = (1.0f / NKF_TRANS_SCALE_1_11_4);

       
        public static BinaryReader LTBFile;
        public static MeshData[] LtbfMesh;
        public static BoneData[] LtbfBone;
        public static AnimData[] LtbfAnim;
      
        public static Int32 Scaleto=255;
        public static UInt32 numMesh = 0;
        public static UInt32 numBones = 0;
        public static UInt32 numAnim = 0;
        public static Boolean isAnim = false;
        public static Boolean isAutoScaler = false;
        public static Boolean isSubForm = false;
        public static Boolean isCalcframe = true;
        public static Boolean isAutoCreateQC = false;
        public static Boolean isCheckboxAnim= true;
        public static Boolean is__doing;
        public static Boolean is_slp_hand=false;
        public static Boolean is_spl_model = false;

        public static List<double[,]> Matrix4x4s = new List<double[,]>();
        public static List<double[,]> MatrixBone = new List<double[,]>();

        public static CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();

        public static List<string> input_ltb = new List<string>();
        OpenFileDialog openFileDialog1 = new OpenFileDialog();
        OpenFileDialog openFileDialog2 = new OpenFileDialog();
        bool is_install_file=false;
        public static string cur_fName = "", cur_Path = "";
        void write_mdllib(String Path)
        {

            if (File.Exists(Path + "studiomdl.exe")) return;
            File.WriteAllBytes(Path+"studiomdl.exe", Properties.Resources.studiomdl);

            FileInfo fi = new FileInfo(Path+"studiomdl.exe");
            fi.Attributes = FileAttributes.Hidden;

      
           
        }
        void write_dll()
        {
            if (is_install_file == true) return;
            is_install_file = true;
            if (!File.Exists("FreeImage.dll"))
            {
                File.WriteAllBytes("FreeImage.dll", Properties.Resources.FreeImage);

                FileInfo fi = new FileInfo("FreeImage.dll");
                fi.Attributes = FileAttributes.Hidden;
            }

            if (Environment.Is64BitProcess == true)
            {
                if (!File.Exists("NativeSquish_x64.dll"))
                {
                    File.WriteAllBytes("NativeSquish_x64.dll", Properties.Resources.NativeSquish_x64);
                    FileInfo fi = new FileInfo("NativeSquish_x64.dll");
                    fi.Attributes = FileAttributes.Hidden;
                }
            }
            else
            {
                if (!File.Exists("NativeSquish_x86.dll"))
                {
                    File.WriteAllBytes("NativeSquish_x86.dll", Properties.Resources.NativeSquish_x86);
                    FileInfo fi = new FileInfo("NativeSquish_x86.dll");
                    fi.Attributes = FileAttributes.Hidden;
                }
            }
         
        }
        public Form1()
        {
           
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                string resourceName = new AssemblyName(args.Name).Name + ".dll";
                string resource = Array.Find(this.GetType().Assembly.GetManifestResourceNames(), element => element.EndsWith(resourceName));
          
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                {
                    Byte[] assemblyData = new Byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            };
            culture.NumberFormat.NumberDecimalSeparator = ".";
            openFileDialog1.Filter = "DTX File|*.dtx";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;
            openFileDialog2.Filter = "LTB File|*.ltb";
            openFileDialog2.FilterIndex = 1;
            openFileDialog2.Multiselect = true;
           

            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            richTextBox1.ScrollToCaret();
        }


        public static float UnpackFromInt16(Int16 intval)
        {
            return (float)(intval) * NKF_TRANS_OOSCALE_1_11_4;
        }
        public static string read_string(BinaryReader gbStream)
        {
            string returndata = "";
            UInt16 nChar = gbStream.ReadUInt16();
            for (int n = 0; n < nChar; n++)
                returndata += gbStream.ReadChar();
            return returndata;
        }

        static void calc_databone()
        {
            Matrix4x4s.Clear();
            for (int i = 0; i < numBones; i++)
            {
                double[,] Matrix4x4 = new double[4, 4];
                for (int k = 0; k < 4; k++)
                {
                    Matrix4x4[k,0] = LtbfBone[i].matdata[k,0];
                    Matrix4x4[k, 1] = LtbfBone[i].matdata[k, 1];
                    Matrix4x4[k, 2] = LtbfBone[i].matdata[k, 2];
                    Matrix4x4[k, 3] = LtbfBone[i].matdata[k, 3];
                }
                Matrix4x4s.Add(Matrix4x4);
                double[,] localMatrix = worldToLocalMatrix(Matrix4x4, LtbfBone[i].par, Matrix4x4s);
                double[] quaternion = new double[3];
                double[] rotation = new double[3];
                quaternion = GetRotation(localMatrix);

                rotation = quaternionToRotation(quaternion);
                //rotation = QuaternionToYawPitchRoll(quaternion);
                double[] position = new double[3];
                position = GetPosition(localMatrix);
                LtbfBone[i].bone_data_out = position[0].ToString("F6", culture) + " " + position[1].ToString("F6", culture) + " " + position[2].ToString("F6", culture) + " " + rotation[0].ToString("F6", culture) + " " + rotation[1].ToString("F6", culture) + " " + rotation[2].ToString("F6", culture);

            }
            
        }
        public static void scale_(int indexanim, float sizemax)
        {
            float nfScale = 1.0f;
           
            float maxframe = (float)LtbfAnim[indexanim].listkeyframe[(int)LtbfAnim[indexanim].nkeyframe - 1];
            if (maxframe > sizemax && isAutoScaler == true)
                nfScale = maxframe / sizemax;
            for (int i = 0; i < LtbfAnim[indexanim].nkeyframe; i++)
            {
                float giTime = (float)LtbfAnim[indexanim].listkeyframe[i] / nfScale;
                if (float.IsNaN(giTime)) giTime = 0.0f;
                LtbfAnim[indexanim].listkeyframe[i] = (int)Math.Round(giTime);

            }
            return;

        }

        static double[,] QuaternionMatrix(double[] quaternion)
        {
             double[,] matrix= new double[4,4];
	        matrix[0,0] = 1.0f - 2.0f * quaternion[1] * quaternion[1] - 2.0f * quaternion[2] * quaternion[2];
	        matrix[1,0] = 2.0f * quaternion[0] * quaternion[1] + 2.0f * quaternion[3] * quaternion[2];
	        matrix[2,0] = 2.0f * quaternion[0] * quaternion[2] - 2.0f * quaternion[3] * quaternion[1];

	        matrix[0,1] = 2.0f * quaternion[0] * quaternion[1] - 2.0f * quaternion[3] * quaternion[2];
	        matrix[1,1] = 1.0f - 2.0f * quaternion[0] * quaternion[0] - 2.0f * quaternion[2] * quaternion[2];
	        matrix[2,1] = 2.0f * quaternion[1] * quaternion[2] + 2.0f * quaternion[3] * quaternion[0];

	        matrix[0,2] = 2.0f * quaternion[0] * quaternion[2] + 2.0f * quaternion[3] * quaternion[1];
	        matrix[1,2] = 2.0f * quaternion[1] * quaternion[2] - 2.0f * quaternion[3] * quaternion[0];
	        matrix[2,2] = 1.0f - 2.0f * quaternion[0] * quaternion[0] - 2.0f * quaternion[1] * quaternion[1];
         
            matrix[3, 0] = 0.0f;
            matrix[3, 1] = 0.0f;
            matrix[3, 2] = 0.0f;
            matrix[3, 3] = 1.0f;
            return matrix;
        }
        static double[,] QuaternionMatrix(float[] quaternion)
        {
            double[,] matrix = new double[4, 4];
            matrix[0, 0] = 1.0f - 2.0f * quaternion[1] * quaternion[1] - 2.0f * quaternion[2] * quaternion[2];
            matrix[1, 0] = 2.0f * quaternion[0] * quaternion[1] + 2.0f * quaternion[3] * quaternion[2];
            matrix[2, 0] = 2.0f * quaternion[0] * quaternion[2] - 2.0f * quaternion[3] * quaternion[1];

            matrix[0, 1] = 2.0f * quaternion[0] * quaternion[1] - 2.0f * quaternion[3] * quaternion[2];
            matrix[1, 1] = 1.0f - 2.0f * quaternion[0] * quaternion[0] - 2.0f * quaternion[2] * quaternion[2];
            matrix[2, 1] = 2.0f * quaternion[1] * quaternion[2] + 2.0f * quaternion[3] * quaternion[0];

            matrix[0, 2] = 2.0f * quaternion[0] * quaternion[2] + 2.0f * quaternion[3] * quaternion[1];
            matrix[1, 2] = 2.0f * quaternion[1] * quaternion[2] - 2.0f * quaternion[3] * quaternion[0];
            matrix[2, 2] = 1.0f - 2.0f * quaternion[0] * quaternion[0] - 2.0f * quaternion[1] * quaternion[1];

            matrix[3, 0] = 0.0f;
            matrix[3, 1] = 0.0f;
            matrix[3, 2] = 0.0f;
            matrix[3, 3] = 1.0f;
            return matrix;
        }
        static double[,] CALC_ConcatTransforms(double[,] in1, double[,] in2)
        {
             double  [,]  gout = new double[4,4];
	        gout[0,0] = in1[0,0] * in2[0,0] + in1[0,1] * in2[1,0] +in1[0,2] * in2[2,0];

	        gout[0,1] = in1[0,0] * in2[0,1] + in1[0,1] * in2[1,1] +
				        in1[0,2] * in2[2,1];
	        gout[0,2] = in1[0,0] * in2[0,2] + in1[0,1] * in2[1,2] +
				        in1[0,2] * in2[2,2];
	        gout[0,3] = in1[0,0] * in2[0,3] + in1[0,1] * in2[1,3] +
				        in1[0,2] * in2[2,3] + in1[0,3];
	        gout[1,0] = in1[1,0] * in2[0,0] + in1[1,1] * in2[1,0] +
				        in1[1,2] * in2[2,0];
	        gout[1,1] = in1[1,0] * in2[0,1] + in1[1,1] * in2[1,1] +
				        in1[1,2] * in2[2,1];
	        gout[1,2] = in1[1,0] * in2[0,2] + in1[1,1] * in2[1,2] +
				        in1[1,2] * in2[2,2];
	        gout[1,3] = in1[1,0] * in2[0,3] + in1[1,1] * in2[1,3] +
				        in1[1,2] * in2[2,3] + in1[1,3];
	        gout[2,0] = in1[2,0] * in2[0,0] + in1[2,1] * in2[1,0] +
				        in1[2,2] * in2[2,0];
	        gout[2,1] = in1[2,0] * in2[0,1] + in1[2,1] * in2[1,1] +
				        in1[2,2] * in2[2,1];
	        gout[2,2] = in1[2,0] * in2[0,2] + in1[2,1] * in2[1,2] +
				        in1[2,2] * in2[2,2];
	        gout[2,3] = in1[2,0] * in2[0,3] + in1[2,1] * in2[1,3] +
				        in1[2,2] * in2[2,3] + in1[2,3];
            gout[3, 0] = 0.0f;
            gout[3, 1] = 0.0f;
            gout[3, 2] = 0.0f;
            gout[3, 3] = 1.0f;
            return gout;
        }
      
        static double DotProduct(float[] x,double[,] y,int var)
        {
            return x[0] * y[var, 0] + x[1] * y[var, 1] + x[2] * y[var,2];
        }

        static float[] VectorTransform(float[] in1, double[,] in2)
        {
            float []gout = new float[3];
	        gout[0] = (float)(DotProduct(in1, in2,0) + in2[0,3]);
	        gout[1] = (float)(DotProduct(in1, in2,1) +	in2[1,3]);
	        gout[2] = (float)(DotProduct(in1, in2,2) +	in2[2,3]);
            return gout;
        }
        static float[] VectorSubtract(float[] va, float[] vb)
       {
           float []gout= new float[3];
            gout[0] = va[0]-vb[0];
	        gout[1] = va[1]-vb[1];
	        gout[2] = va[2]-vb[2];
            return gout;
       }

        static float[] _VectorAdd(float[] va, float[] vb)
        {
            float[] gout=new float[3];
	        gout[0] = va[0]+vb[0];
	        gout[1] = va[1]+vb[1];
	        gout[2] = va[2]+vb[2];
            return gout;
        }
        static float[] MulMatVec(float[] v, double[,] m) 
        {
            float []result=new float[3];
            result[0] =(float)( m[0 , 0] * v[0] + m[0 , 1] * v[1] + m[0 , 2] *v[2] + m[0 , 3]) ;
            result[1] = (float)(m[1 , 0] * v[0] + m[1 , 1] * v[1] + m[1 , 2] * v[2] + m[1 , 3]) ;
            result[2] = (float)(m[2 , 0] * v[0] + m[2 , 1] * v[1]+ m[2 , 2] * v[2] + m[2 , 3] );
                return result;
        }


   static   double[,] InverseMat2(double[,] m)
{
    double[,]invOut = new double [4,4];
    double[,] inv = new double[4, 4];
    double det;
    int i;
 
    inv[0,0] =  m[1,1] * m[2,2] * m[3,3] - m[1,1] * m[2,3] * m[3,2] - m[2,1] * m[1,2] * m[3,3] + m[2,1] * m[1,3] * m[3,2] + m[3,1] * m[1,2] * m[2,3] - m[3,1] * m[1,3] * m[2,2];
    inv[1,0] = -m[1,0] * m[2,2] * m[3,3] + m[1,0] * m[2,3] * m[3,2] + m[2,0] * m[1,2] * m[3,3] - m[2,0] * m[1,3] * m[3,2] - m[3,0] * m[1,2] * m[2,3] + m[3,0] * m[1,3] * m[2,2];
    inv[2,0] =  m[1,0] * m[2,1] * m[3,3] - m[1,0] * m[2,3] * m[3,1] - m[2,0] * m[1,1] * m[3,3] + m[2,0] * m[1,3] * m[3,1] + m[3,0] * m[1,1] * m[2,3] - m[3,0] * m[1,3] * m[2,1];
    inv[3,0] = -m[1,0] * m[2,1] * m[3,2] + m[1,0] * m[2,2] * m[3,1] + m[2,0] * m[1,1] * m[3,2] - m[2,0] * m[1,2] * m[3,1] - m[3,0] * m[1,1] * m[2,2] + m[3,0] * m[1,2] * m[2,1];
    inv[0,1] = -m[0,1] * m[2,2] * m[3,3] + m[0,1] * m[2,3] * m[3,2] + m[2,1] * m[0,2] * m[3,3] - m[2,1] * m[0,3] * m[3,2] - m[3,1] * m[0,2] * m[2,3] + m[3,1] * m[0,3] * m[2,2];
    inv[1,1] =  m[0,0] * m[2,2] * m[3,3] - m[0,0] * m[2,3] * m[3,2] - m[2,0] * m[0,2] * m[3,3] + m[2,0] * m[0,3] * m[3,2] + m[3,0] * m[0,2] * m[2,3] - m[3,0] * m[0,3] * m[2,2];
    inv[2,1] = -m[0,0] * m[2,1] * m[3,3] + m[0,0] * m[2,3] * m[3,1] + m[2,0] * m[0,1] * m[3,3] - m[2,0] * m[0,3] * m[3,1] - m[3,0] * m[0,1] * m[2,3] + m[3,0] * m[0,3] * m[2,1];
    inv[3,1] =  m[0,0] * m[2,1] * m[3,2] - m[0,0] * m[2,2] * m[3,1] - m[2,0] * m[0,1] * m[3,2] + m[2,0] * m[0,2] * m[3,1] + m[3,0] * m[0,1] * m[2,2] - m[3,0] * m[0,2] * m[2,1];
    inv[0,2] =  m[0,1] * m[1,2] * m[3,3] - m[0,1] * m[1,3] * m[3,2] - m[1,1] * m[0,2] * m[3,3] + m[1,1] * m[0,3] * m[3,2] + m[3,1] * m[0,2] * m[1,3] - m[3,1] * m[0,3] * m[1,2];
    inv[1,2] = -m[0,0] * m[1,2] * m[3,3] + m[0,0] * m[1,3] * m[3,2] + m[1,0] * m[0,2] * m[3,3] - m[1,0] * m[0,3] * m[3,2] - m[3,0] * m[0,2] * m[1,3] + m[3,0] * m[0,3] * m[1,2];
    inv[2,2] =  m[0,0] * m[1,1] * m[3,3] - m[0,0] * m[1,3] * m[3,1] - m[1,0] * m[0,1] * m[3,3] + m[1,0] * m[0,3] * m[3,1] + m[3,0] * m[0,1] * m[1,3] - m[3,0] * m[0,3] * m[1,1];
    inv[2,3] = -m[0,0] * m[1,1] * m[3,2] + m[0,0] * m[1,2] * m[3,1] + m[1,0] * m[0,1] * m[3,2] - m[1,0] * m[0,2] * m[3,1] - m[3,0] * m[0,1] * m[1,2] + m[3,0] * m[0,2] * m[1,1];
    inv[0,3] = -m[0,1] * m[1,2] * m[2,3] + m[0,1] * m[1,3] * m[2,2] + m[1,1] * m[0,2] * m[2,3] - m[1,1] * m[0,3] * m[2,2] - m[2,1] * m[0,2] * m[1,3] + m[2,1] * m[0,3] * m[1,2];
    inv[1,3] =  m[0,0] * m[1,2] * m[2,3] - m[0,0] * m[1,3] * m[2,2] - m[1,0] * m[0,2] * m[2,3] + m[1,0] * m[0,3] * m[2,2] + m[2,0] * m[0,2] * m[1,3] - m[2,0] * m[0,3] * m[1,2];
    inv[2,3] = -m[0,0] * m[1,1] * m[2,3] + m[0,0] * m[1,3] * m[2,1] + m[1,0] * m[0,1] * m[2,3] - m[1,0] * m[0,3] * m[2,1] - m[2,0] * m[0,1] * m[1,3] + m[2,0] * m[0,3] * m[1,1];
    inv[3,3] =  m[0,0] * m[1,1] * m[2,2] - m[0,0] * m[1,2] * m[2,1] - m[1,0] * m[0,1] * m[2,2] + m[1,0] * m[0,2] * m[2,1] + m[2,0] * m[0,1] * m[1,2] - m[2,0] * m[0,2] * m[1,1];
 
    det = m[0,0] * inv[0,0] + m[0,1] * inv[1,0] + m[0,2] * inv[2,0] + m[0,3] * inv[3,0];
 
    if(det == 0)
        return invOut;
 
    det = 1.0f / det;
 
    for(i = 0; i < 4; i++)
		for(int j = 0; j < 4;j++)
        invOut[i,j] = inv[i,j] * det;

    return invOut;
}
        static double[,] InverseMat(  double[,] Matrix)
        {
            double[,]  UnitMatrix=new double[4,4];
            double[,] inverseMatrix = new double[4, 4];
            for (int i = 0; i < 4; i++)
            {
                UnitMatrix[i, i] = 1;
            }
            for (int i = 0; i < 4; i++)
            {
                double[] roots = solveEquations(new double[4, 5] { { Matrix[0, 0], Matrix[1, 0], Matrix[2, 0], Matrix[3, 0], UnitMatrix[i, 0] }, { Matrix[0, 1], Matrix[1, 1], Matrix[2, 1], Matrix[3, 1], UnitMatrix[i, 1] }, { Matrix[0, 2], Matrix[1, 2], Matrix[2, 2], Matrix[3, 2], UnitMatrix[i, 2] }, { Matrix[0, 3], Matrix[1, 3], Matrix[2, 3], Matrix[3, 3], UnitMatrix[i, 3] } });
                for (int j = 0; j < 4; j++)
                {
                    inverseMatrix[i, j] = roots[j];
                }
            }
            return inverseMatrix;
        }
        static float[] VectorIRotate(float[] in1, double[,] in2)
        {
            float[] gout = new float[3];
            gout[0] = (float )(in1[0]*in2[0,0] + in1[1]*in2[1,0] + in1[2]*in2[2,0]);
            gout[1] = (float )(in1[0]*in2[0,1] + in1[1]*in2[1,1] + in1[2]*in2[2,1]);
            gout[2] = (float )(in1[0] * in2[0,2] + in1[1] * in2[1,2] + in1[2] * in2[2,2]);
            return gout;
        }
       static float[] VectorRotate (float[] in1, double[,] in2)
        {
            float[] gout = new float[3];
            gout[0] = (float)DotProduct(in1, in2, 0);
            gout[1] = (float)DotProduct(in1, in2, 1);
            gout[2] = (float)DotProduct(in1, in2, 2);
            return gout;
        }
        static double normal_rotate(double  angle)
        {
            while (angle > 2 * (Math.PI)) angle -= 2 * (Math.PI);
            while (angle < 2 * (Math.PI)) angle += 2 * (Math.PI);
            return angle;

        }
       static  double[] rotationMatrixToEulerAngles(double[,] matrix)
        {
          
            double sy = Math.Sqrt(matrix[0, 0] * matrix[0, 0] + matrix[1, 0] * matrix[1, 0]);

            bool singular = sy < 1e-6; // If

            double x, y, z;
            if (!singular)
            {
                x = Math.Atan2(matrix[2, 1], matrix[2, 2]);
                y = Math.Atan2(-matrix[2, 0], sy);
                z = Math.Atan2(matrix[1, 0], matrix[0, 0]);
            }
            else
            {

                x = Math.Atan2(matrix[1, 2], matrix[1, 1]);
                y = Math.Atan2(-matrix[2, 0], sy);
                z = 0;

                if (matrix[2, 0] > 0 && matrix[0, 1] > 0)
                {
                    x = Math.Atan2(matrix[1, 2], matrix[1, 1]);
                    y = Math.Atan2(-matrix[2, 0], sy);
                    z = Math.PI;
                }
            }

            normal_rotate(x);
            normal_rotate(y);
            normal_rotate(z);
            return new double[] { x,y,z};
 
        }
        static void Calc_Bone_Transform(AnimData gAnim,int isframe)
        {
            
                MatrixBone.Clear();
                for (int i = 0; i < numBones; i++)
                {
                    int gpar = LtbfBone[i].par;
                    double[,] matrix;
                    matrix = QuaternionMatrix(gAnim.frame[i].quats[isframe]);
                    matrix[0, 3] = gAnim.frame[i].pos[isframe][0];
                    matrix[1, 3] = gAnim.frame[i].pos[isframe][1];
                    matrix[2, 3] = gAnim.frame[i].pos[isframe][2];
                    if (gpar > -1)
                    {
                        matrix = CALC_ConcatTransforms(MatrixBone[gpar], matrix);
                    }

                    MatrixBone.Add(matrix);
                }
        }
        static void get_new_bone_out_data(int indexanim = 0, int frame = 0,float gScale_x=1.0f,float gScale_y=1.0f,float gScale_z=0.65f)
        {
            List<double[,]> MatrixL=new List<double[,]>();
            Calc_Bone_Transform(LtbfAnim[indexanim], frame);

            for (int i = 0; i < numBones; i++)
            {
                double[,] Matrix4x4 = new double[4, 4];
                for (int k = 0; k < 4; k++)
                {
                    Matrix4x4[k, 0] = MatrixBone[i][k, 0];
                    Matrix4x4[k, 1] = MatrixBone[i][k, 1];
                    Matrix4x4[k, 2] = MatrixBone[i][k, 2];
                    Matrix4x4[k, 3] = MatrixBone[i][k, 3];
                }
                Matrix4x4[0,3]*=gScale_x;
                Matrix4x4[1,3]*=gScale_y;
                Matrix4x4[2,3]*=gScale_z;
                MatrixL.Add(Matrix4x4);
                double[,] localMatrix = worldToLocalMatrix(Matrix4x4, LtbfBone[i].par, MatrixL);
                //double[] quaternion = new double[3];
                double[] rotation = new double[3];
              //  quaternion = GetRotation(localMatrix);




                rotation = rotationMatrixToEulerAngles(localMatrix);

                //rotation = QuaternionToYawPitchRoll(quaternion);
                double[] position = new double[3];
                position = GetPosition(localMatrix);
                LtbfBone[i].bone_data_out = position[0].ToString("F6", culture) + " " + position[1].ToString("F6", culture) + " " + position[2].ToString("F6", culture) + " " + rotation[0].ToString("F6", culture) + " " + rotation[1].ToString("F6", culture) + " " + rotation[2].ToString("F6", culture);
            }
          //  MatrixBone.Clear();
            Change_a_mesh(gScale_x,gScale_y,gScale_z);
            MatrixBone.Clear();
        }
        static void Change_a_mesh(float gScale_x = 1.0f, float gScale_y = 1.0f, float gScale_z = 0.5f)
        {
            for (int i=0;i<numMesh;i++)
                LtbfMesh[i] = get_new_MeshData(i, gScale_x, gScale_y, gScale_z);

        }
        static void Change_a_anim(float gScale_x = 1.0f, float gScale_y = 1.0f, float gScale_z = 0.5f)
        {
            for (int i = 0; i < numAnim; i++)
                LtbfAnim[i]= get_animm(i, gScale_x, gScale_y , gScale_z);
        }

        static AnimData get_animm(int gAnim, float gScale_x = 1.0f, float gScale_y = 1.0f, float gScale_z = 0.5f)
        {
            AnimData outdata;
            outdata = LtbfAnim[gAnim];

            for (int i = 0; i < outdata.nkeyframe; i++)
            {
                List<double[,]> MatrixL = new List<double[,]>();
                MatrixBone.Clear();
                Calc_Bone_Transform(outdata, i);

                for (int k = 0; k < numBones; k++)
                {
                  //  double[] quaternion = new double[4] { gAnim.frame[k].quats[i][0], gAnim.frame[k].quats[i][1], gAnim.frame[k].quats[i][2], gAnim.frame[k].quats[i][3] };
                    double[,] Matrix4x4 = new double[4, 4];
                    for (int n = 0; n < 4; n++)
                    {
                        Matrix4x4[n, 0] = MatrixBone[k][n, 0];
                        Matrix4x4[n, 1] = MatrixBone[k][n, 1];
                        Matrix4x4[n, 2] = MatrixBone[k][n, 2];
                        Matrix4x4[n, 3] = MatrixBone[k][n, 3];
                    }
                    Matrix4x4[0, 3] *= gScale_x;
                    Matrix4x4[1, 3] *= gScale_y;
                    Matrix4x4[2, 3] *= gScale_z;
                    MatrixL.Add(Matrix4x4);
                    double[,] localMatrix = worldToLocalMatrix(Matrix4x4, LtbfBone[k].par, MatrixL);
                    double[] quaternion = new double[3];
                    double[] rotation = new double[3];
                    quaternion = GetRotation(localMatrix);
                    outdata.frame[k].quats[i] = new float[] { (float)quaternion[0], (float)quaternion[1], (float)quaternion[2], (float)quaternion[3] };
                    double[] position = new double[3];
                    position = GetPosition(localMatrix);
                    outdata.frame[k].pos[i] = new float[] { (float)position[0], (float)position[1], (float)position[2] };
                }
            }
            return outdata;
        }

        static MeshData get_new_MeshData(int gMEsh, float gScale_x = 1.0f, float gScale_y = 1.0f, float gScale_z = 0.5f)
        {
            MeshData outmesh;
            outmesh = LtbfMesh[gMEsh];

            List<float[]> vertices = new List<float[]>();
            List<float[]> normals = new List<float[]>();
            for (int i = 0; i < outmesh.nvertices; i++)
            {
                    int gbone = outmesh.weightsets_output[i];
                    float[] vecvert = new float[3];
                    vecvert[0] = outmesh.vertices[i][0] ;
                    vecvert[1] = outmesh.vertices[i][1] ;
                    vecvert[2] = outmesh.vertices[i][2] ;
                    float[] newvert = VectorTransform(vecvert, InverseMat2(LtbfBone[gbone].matdata));
                    float[] newvert2 = MulMatVec(newvert, MatrixBone[gbone]);
                    float[] normal = new float[3];
                    normal[0] = outmesh.normals[i][0];
                    normal[1] = outmesh.normals[i][1];
                    normal[2] = outmesh.normals[i][2];
                    float[] newnormal = VectorRotate(normal, InverseMat2(LtbfBone[gbone].matdata));
                    float[] newnormal2 = VectorRotate(newnormal, MatrixBone[gbone]);
                    newvert2[0] *= gScale_x;
                    newvert2[1] *= gScale_y;
                    newvert2[2] *= gScale_z;
                    vertices.Add(newvert2);
                    normals.Add(newnormal2);
            }
            outmesh.vertices = vertices;
            outmesh.normals = normals;
                return outmesh;
        }
        static void test_transform()
        {
            StreamWriter smd;
            FileStream smdStr = new FileStream("c:\\mesh3.smd", FileMode.Create, FileAccess.Write);
            smd = new StreamWriter(smdStr);
            smd.Write("version 1\nnodes\n");

            for (int i2 = 0; i2 < numBones; i2++)
            {
                smd.Write(" " + i2 + "  \"" + LtbfBone[i2].name + "\" -1\n");
            }
            smd.Write("end\n\nskeleton\n");

            int ianim =2,isframe=0;
          //  for (isframe = 0; isframe < LtbfAnim[ianim].nkeyframe; isframe++)
            {
                MatrixBone.Clear();
                for (int i = 0; i < numBones; i++)
                {
                    int gpar = LtbfBone[i].par;
                    double[,] matrix;
                    matrix = QuaternionMatrix(LtbfAnim[ianim].frame[i].quats[isframe]);
                    matrix[0, 3] = LtbfAnim[ianim].frame[i].pos[isframe][0];
                    matrix[1, 3] = LtbfAnim[ianim].frame[i].pos[isframe][1];
                    matrix[2, 3] = LtbfAnim[ianim].frame[i].pos[isframe][2];
                    if (gpar > -1)
                    {
                        matrix = CALC_ConcatTransforms(MatrixBone[gpar], matrix);
                    }

                    MatrixBone.Add(matrix);
                }

                  int ismesh = 1;
                  List<float[]> vertices=new List<float[]>();
                  List<float[]> vect_sub = new List<float[]>();
                  for (int i = 0; i < LtbfMesh[ismesh].nvertices; i++)
                  {
                      //vec3_t tmp;
                      //VectorScale (pstudioverts[i], 12, tmp);
                      int gbone = LtbfMesh[ismesh].weightsets_output[i];
                   
                      float[] vecvert = new float[3];

                      float[] bonever = new float[] { (float)LtbfBone[gbone].matdata[0, 3], (float)LtbfBone[gbone].matdata[1, 3], (float)LtbfBone[gbone].matdata[2, 3] };
                      vecvert[0] =LtbfMesh[ismesh].vertices[i][0] ;
                      vecvert[1] =LtbfMesh[ismesh].vertices[i][1] ;
                      vecvert[2] =LtbfMesh[ismesh].vertices[i][2] ;
                     // Matrix4x4s
                      float[] newvert = VectorTransform(vecvert, InverseMat2( LtbfBone[gbone].matdata ) );

                      float[] newvert2 = MulMatVec(newvert, MatrixBone[gbone]);
                    //  newvert[0] += (float)MatrixBone[gbone][0, 3];
                    //  newvert[1] += (float)MatrixBone[gbone][1, 3];
                    //  newvert[2] += (float)MatrixBone[gbone][2, 3];
                      vertices.Add(newvert2);
                  }
                  Write_MODEL("c:\\mesh2.smd", vertices,ismesh);

                smd.Write("time " + isframe + "\n");
                for (int i2 = 0; i2 < numBones; i2++)
                {
                    smd.Write(" " + i2 + "  " + MatrixBone[i2][0, 3].ToString("F6") + " " + MatrixBone[i2][1, 3].ToString("F6") + " " + MatrixBone[i2][2, 3].ToString("F6") + "0.000000 0.000000 0.000000" + "\n");

                }
                // smd.Write("end\ntriangles\n");
                //smd.Write("a.bmp\n");
                //  smd.Write("0 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000\n");
                //  smd.Write("0 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000\n");
                // smd.Write("0 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000\n");
              
            }
            smd.Write("end");
            smd.Close();

        }
        public static void Write_MODEL(string tofile, List<float[]> vertices,int i)
        {

            StreamWriter smd;
            FileStream smdStr = new FileStream(tofile, FileMode.Create, FileAccess.Write);
            smd = new StreamWriter(smdStr);
            smd.Write("version 1\nnodes\n");

            for (int i2 = 0; i2 < numBones; i2++)
            {
                smd.Write(" " + i2 + "  \"" + LtbfBone[i2].name + "\" " + LtbfBone[i2].par + "\n");
            }
            smd.Write("end\n\nskeleton\ntime 0\n");

            for (int i2 = 0; i2 < numBones; i2++)
            {
                smd.Write(" " + i2 + "  " + LtbfBone[i2].bone_data_out + "\n");
            }
            smd.Write("end\ntriangles\n");
            for (int j = 0; j < LtbfMesh[i].nIdx; j += 3)
            {
                int tr = LtbfMesh[i].triangles[j];
                int tr1 = LtbfMesh[i].triangles[j + 1];
                int tr2 = LtbfMesh[i].triangles[j + 2];
                smd.Write(LtbfMesh[i].name.Replace(" ", "_") + ".bmp\n");
                smd.Write(LtbfMesh[i].weightsets_output[tr] + " " +vertices[tr][0].ToString("F6", culture) + " " +vertices[tr][1].ToString("F6", culture) + " " +vertices[tr][2].ToString("F6", culture) + " "
                + LtbfMesh[i].normals[tr][0].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr][1].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr][2].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr][0].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr][1].ToString("F6", culture) + "\n");
               
                smd.Write(LtbfMesh[i].weightsets_output[tr1] + " " + vertices[tr1][0].ToString("F6", culture) + " " + vertices[tr1][1].ToString("F6", culture) + " " + vertices[tr1][2].ToString("F6", culture) + " "
                + LtbfMesh[i].normals[tr1][0].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr1][1].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr1][2].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr1][0].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr1][1].ToString("F6", culture) + "\n");
               
                smd.Write(LtbfMesh[i].weightsets_output[tr2] + " " + vertices[tr2][0].ToString("F6", culture) + " " + vertices[tr2][1].ToString("F6", culture) + " " + vertices[tr2][2].ToString("F6", culture) + " "
                + LtbfMesh[i].normals[tr2][0].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr2][1].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr2][2].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr2][0].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr2][1].ToString("F6", culture) + "\n");
                
            }
            smd.Write("end");
            smd.Close();
        }


        static float[]  QuaternionSlerp(float[] p, float[] q, float t)

            {
                  float [] qt = new float[4];
	            int i;
	            double omega, cosom, sinom, sclp, sclq;

	            // decide if one of the quaternions is backwards
	            float a = 0;
	            float b = 0;
	            for (i = 0; i < 4; i++) {
		            a += (p[i]-q[i])*(p[i]-q[i]);
		            b += (p[i]+q[i])*(p[i]+q[i]);
	            }
	            if (a > b) {
		            for (i = 0; i < 4; i++) {
			            q[i] = -q[i];
		            }
	            }

	            cosom = p[0]*q[0] + p[1]*q[1] + p[2]*q[2] + p[3]*q[3];

	            if ((1.0 + cosom) > 0.00000001f) {
		            if ((1.0 - cosom) > 0.00000001f) {
			            omega = Math.Acos( cosom );
			            sinom = Math.Sin( omega );
			            sclp = Math.Sin( (1.0f - t)*omega) / sinom;
			            sclq = Math.Sin( t*omega ) / sinom;
		            }
		            else {
			            sclp = 1.0f - t;
			            sclq = t;
		            }
		            for (i = 0; i < 4; i++) {
			            qt[i] = (float)(sclp * p[i] + sclq * q[i]);
		            }
	            }
	            else {
		            qt[0] = -p[1];
		            qt[1] = p[0];
		            qt[2] = -p[3];
		            qt[3] = p[2];
		            sclp = Math.Sin( (1.0f - t) * 0.5f * Math.PI);
		            sclq = Math.Sin( t * 0.5f * Math.PI);
		            for (i = 0; i < 3; i++) {
			            qt[i] =  (float)(sclp * p[i] + sclq * qt[i]);
		            }
	            }
                return qt;
            }



        public static void auto_calc_all_Frame(int indexanim)
        {
            if (isSubForm == false) return;
            scale_(indexanim, Scaleto);

            if (isCalcframe == false) return;
            List<int> glistkeyframe = LtbfAnim[indexanim].listkeyframe;
            framedata[] frame = LtbfAnim[indexanim].frame;
            List<int> newlistframe = new List<int>();
            framedata[] newframe = new framedata[numBones];

            for (int j = 0; j < numBones; j++)
            {
                newframe[j].pos = new List<float[]>();
                newframe[j].quats = new List<float[]>();

            }
            for (int j = 0; j < glistkeyframe[glistkeyframe.Count - 1]; j++)
                newlistframe.Add(j);
            for (int i = 1; i < LtbfAnim[indexanim].nkeyframe; i++)
            {
                for (int j = 0; j < numBones; j++)
                {
                    int nF = glistkeyframe[i] - glistkeyframe[i - 1];
                    float[] add_pos = new float[3];
                    float[] add_quats = new float[4];
                    for (int n = 0; n < 3; n++)
                        add_pos[n] = (frame[j].pos[i - 1][n] -frame[j].pos[i][n]) / ((float)nF);

                   // for (int n = 0; n < 4; n++)
                 //       add_quats[n] = (frame[j].quats[i - 1][n] - frame[j].quats[i][n]) / ((float)nF);

                    for (int k = glistkeyframe[i - 1]; k < glistkeyframe[i]-1; k++)
                    {
                        float[] pos = new float[3];
                        float[] quats = new float[4];

                        float[] rot = new float[3];
                        for (int n = 0; n < 3; n++)
                            
                            pos[n] = frame[j].pos[i - 1][n] + add_pos[n] * ( glistkeyframe[i - 1]-k);

                        float time =  (float)(k - glistkeyframe[i - 1]) / (float)nF;
                        if (time > 1.0f) time = 1.0f;
                        quats = QuaternionSlerp(frame[j].quats[i - 1], frame[j].quats[i], time);
                           
                        
                       // if (quats[3] > 1.0f) quats[3] -= 1.0f;
                      //  quats[3]
                     //   float lenght = (float)Math.Sqrt(quats[0] * quats[0] + quats[1] * quats[1] + quats[2] * quats[2]);
                       

                        newframe[j].pos.Add(pos);
                        newframe[j].quats.Add(quats);
                    }
                    newframe[j].pos.Add(frame[j].pos[i]);
                    newframe[j].quats.Add(frame[j].quats[i]);
                    
                }

            }
            LtbfAnim[indexanim].listkeyframe.Clear();
            LtbfAnim[indexanim].listkeyframe = newlistframe;
            LtbfAnim[indexanim].nkeyframe = Convert.ToUInt32(newlistframe.Count);
            LtbfAnim[indexanim].frame = newframe;
        }

        public static void Write_QC(string tofile,string modelname)
        {
           // test_transform();
            StreamWriter qcfile;
            FileStream QCStr = new FileStream(tofile, FileMode.Create, FileAccess.Write);
            qcfile = new StreamWriter(QCStr);
            qcfile.WriteLine("/*");
            qcfile.WriteLine("File QC - Duoc tao boi tools Convert LTB to SMD <Author:Giay Nhap>");
            qcfile.WriteLine("Fb:fb.com\\abcGiayNhapcba");
            qcfile.WriteLine("*/");
            qcfile.WriteLine("");
            qcfile.WriteLine("$modelname \"" + modelname + ".mdl\"");
            qcfile.WriteLine("$cd \"" + cur_Path + "\\\""); 
            qcfile.WriteLine("$cdtexture \".\\\"");
            qcfile.WriteLine("$scale 1.0");
            qcfile.WriteLine("$cliptotextures");
            qcfile.WriteLine("");
            qcfile.WriteLine("$bbox 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000");
            qcfile.WriteLine("$cbox 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000");
            qcfile.WriteLine("$eyeposition 0.000000 0.000000 0.000000");
            qcfile.WriteLine("//$origin 0.000000 0.000000 0.000000");
            qcfile.WriteLine("//$rotate 0.000000 0.000000 0.000000");
            qcfile.WriteLine("$scale 0.4");
            qcfile.WriteLine("");
            qcfile.WriteLine("$body \"gn_mesh\" \"" + modelname + "\" ");
            qcfile.WriteLine("");
            for (int i=0;i < numAnim;i++)
            {
               qcfile.Write("$sequence \"" + LtbfAnim[i].name + "\" \"" + LtbfAnim[i].name + "\" fps " + LtbfAnim[i].listkeyframe[(int)LtbfAnim[i].nkeyframe - 1]);
               if ( LtbfAnim[i].name.IndexOf("idle")!=-1 || LtbfAnim[i].name.IndexOf("run")!=-1)  qcfile.WriteLine(" loop");
               else qcfile.WriteLine("");
            }

            qcfile.Close();
        }
        static void write_list_textures(string tofile)
        {
            StreamWriter text_file;
            FileStream text_filetr = new FileStream(tofile, FileMode.Create, FileAccess.Write);
            text_file = new StreamWriter(text_filetr);
            for (int i = 0; i < numMesh; i++)
            {
                text_file.WriteLine(LtbfMesh[i].name.Replace(" ", "_") + ".bmp\n");
            }
            text_file.Close();
        }
        public static void Write_SMD_ANIM(int indexanim, string toFile)
        {
            StreamWriter smd;
            FileStream smdStr = new FileStream(toFile, FileMode.Create, FileAccess.Write);
            smd = new StreamWriter(smdStr);

            auto_calc_all_Frame(indexanim);

            smd.Write("version 1\nnodes\n");
            float gScan = 1.0f;
            for (int i = 0; i < numBones; i++)
            {
                smd.Write(" " + i + "  \"" + LtbfBone[i].name + "\" " + LtbfBone[i].par + "\n");
            }
            smd.Write("end\nskeleton\n");

            for (int i = 0; i < LtbfAnim[indexanim].nkeyframe; i++)
            {
                float giTime = (float)LtbfAnim[indexanim].listkeyframe[i] / gScan;
                if (float.IsNaN(giTime)) giTime = 0.0f;

                smd.Write("time " + Math.Round(giTime) + "\n");

                for (int k = 0; k < numBones; k++)
                {
                    double[] quaternion = new double[4] { LtbfAnim[indexanim].frame[k].quats[i][0], LtbfAnim[indexanim].frame[k].quats[i][1], LtbfAnim[indexanim].frame[k].quats[i][2], LtbfAnim[indexanim].frame[k].quats[i][3] };
                    double[,] matrix;


                    double length = Math.Sqrt(quaternion[0] * quaternion[0] + quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2] + quaternion[3] * quaternion[3]);
                    quaternion[0] /= length;
                    quaternion[1] /= length;
                    quaternion[2] /= length;
                    quaternion[3] /= length;

                    matrix = QuaternionMatrix(quaternion);
                    double[] rotation = new double[3];




                    rotation = rotationMatrixToEulerAngles(matrix);


                  
                  //  rotation = quaternionToRotation(quaternion);
                   // rotation = quaternionToRotation(quaternion);
                    
                    float[] position = new float[3];
                    position[0] = LtbfAnim[indexanim].frame[k].pos[i][0];
                    position[1] = LtbfAnim[indexanim].frame[k].pos[i][1];
                    position[2] = LtbfAnim[indexanim].frame[k].pos[i][2];
                    if (LtbfBone[k].par == -1) rotation[0] -= Math.PI / 2.0f;
                    smd.Write(k + "   " + position[0].ToString("F6", culture) + " " + position[1].ToString("F6", culture) + " " + position[2].ToString("F6", culture) + " " + rotation[0].ToString("F6", culture) + " " + rotation[1].ToString("F6", culture) + " " + rotation[2].ToString("F6", culture) + "\n");
                }
            }
            smd.Write("end");
            smd.Close();
        }
        static void Scale_()
        {
            for (int i = 0; i < numMesh; i++)
            {
                for (int j = 0; j < LtbfMesh[i].nvertices; j++)
                {

                    LtbfMesh[i].vertices[i][2] *= 0.7f;
                }
            }
            for (int i = 0; i < numBones; i++)
            {
                LtbfBone[i].matdata[2, 3] *= 0.7f;
            }
        }
        static void write_model_header( StreamWriter smd)
        {
           

             smd.WriteLine("version 1");
             smd.WriteLine("nodes");
            for (int i = 0; i < numBones; i++)
            {
                smd.WriteLine(i + " \"" + LtbfBone[i].name + "\" " + LtbfBone[i].par);
            }
           
            smd.WriteLine("end");
            smd.WriteLine("skeleton");
            smd.WriteLine("time 0");
            for (int i = 0; i < numBones; i++)
            {
                smd.WriteLine(i + " " + LtbfBone[i].bone_data_out );
            }
            smd.WriteLine("end");
            smd.WriteLine("triangles");
        }
        public static void Write_SMD_MODEL(string tofile)
        {
         //   0962005339
            write_list_textures(tofile+"_LIST_TEX.txt");
            int MAXSTUDIOVERTS = 2000;
            StreamWriter smd;
            FileStream smdStr = new FileStream(tofile+".smd", FileMode.Create, FileAccess.Write);
            smd = new StreamWriter(smdStr);
            write_model_header(smd);
            int num_f_brak = 0;
            int had_poly=0;
            for (int i = 0; i < numMesh; i++)
            {

                if (i == 2 && is_slp_hand==true)
                {
                    smd.WriteLine("end");
                    smd.WriteLine("");
                    smd.Close();
                    smdStr = new FileStream(tofile + "_" + num_f_brak + ".smd", FileMode.Create, FileAccess.Write);
                    smd = new StreamWriter(smdStr);
                    write_model_header(smd);
                    num_f_brak += 1;
                }

                for (int j = 0; j < LtbfMesh[i].nIdx; j += 3)
                {
                    if (is_spl_model == true)
                    {
                        if ((j - had_poly+3) / 3 > MAXSTUDIOVERTS)
                        {
                            had_poly += j;
                            smd.WriteLine("end");
                            smd.WriteLine("");
                            smd.Close();
                            smdStr = new FileStream(tofile + "_" + num_f_brak + ".smd", FileMode.Create, FileAccess.Write);
                            smd = new StreamWriter(smdStr);
                            write_model_header(smd);
                            num_f_brak += 1;
                        }
                    }
                    int tr = LtbfMesh[i].triangles[j];
                    int tr1 = LtbfMesh[i].triangles[j + 1];
                    int tr2 = LtbfMesh[i].triangles[j + 2];
                    smd.WriteLine(LtbfMesh[i].name.Replace(" ","_") + ".bmp");
                    smd.WriteLine(LtbfMesh[i].weightsets_output[tr] + " " + LtbfMesh[i].vertices[tr][0].ToString("F6", culture) + " " + LtbfMesh[i].vertices[tr][1].ToString("F6", culture) + " " + LtbfMesh[i].vertices[tr][2].ToString("F6", culture) + " "
                    + LtbfMesh[i].normals[tr][0].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr][1].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr][2].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr][0].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr][1].ToString("F6", culture) );
                    smd.WriteLine(LtbfMesh[i].weightsets_output[tr1] + " " + LtbfMesh[i].vertices[tr1][0].ToString("F6", culture) + " " + LtbfMesh[i].vertices[tr1][1].ToString("F6", culture) + " " + LtbfMesh[i].vertices[tr1][2].ToString("F6", culture) + " "
                   + LtbfMesh[i].normals[tr1][0].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr1][1].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr1][2].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr1][0].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr1][1].ToString("F6", culture) );
                    smd.WriteLine(LtbfMesh[i].weightsets_output[tr2] + " " + LtbfMesh[i].vertices[tr2][0].ToString("F6", culture) + " " + LtbfMesh[i].vertices[tr2][1].ToString("F6", culture) + " " + LtbfMesh[i].vertices[tr2][2].ToString("F6", culture) + " "
                   + LtbfMesh[i].normals[tr2][0].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr2][1].ToString("F6", culture) + " " + LtbfMesh[i].normals[tr2][2].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr2][0].ToString("F6", culture) + " " + LtbfMesh[i].uvs[tr2][1].ToString("F6", culture));
              
                }
            }
            smd.WriteLine("end");
            smd.WriteLine("");
            smd.Close();
        }

        static int GetBoneWeightD(int[] num, float[] size)
        {

            float max = size[0];
            int boneWeight = num[1];
            if (num[0] > 4) num[0] = 4;
            for (int i = 1; i < num[0]; i++)
            {

                if (max < size[i])
                {
                    max = size[i];
                    boneWeight = num[i + 1];
                }
            }
            return boneWeight;
        }
        public static void Clac_Par_Bone()
        {
            uint[] nsubone = new uint[numBones];
            nsubone[0] = LtbfBone[0].nSubbone;

            LtbfBone[0].par = -1;
            for (int i = 1; i < numBones; i++)
            {
                nsubone[i] = LtbfBone[i].nSubbone;
                for (int j = i - 1; j >= 0; j--)
                    if (nsubone[j] > 0)
                    {
                        nsubone[j] -= 1;
                        LtbfBone[i].par = j;
                        break;
                    }
            }
        }
        public static void Calc_weightsets()
        {
            for (int i = 0; i < numMesh; i++)
            {

                if (LtbfMesh[i].weightsets.Count > 1)
                {
                    int pWeightset = 0;
                    for (int j = 0; j < LtbfMesh[i].weightsets.Count; j++)
                    {
                        int[] intWeightSet = new int[5];

                        int num = 0;
                        for (int n = 0; n < 4; n++)
                        {
                            if (LtbfMesh[i].weightsets[j][2 + n] > -1)
                            {
                                intWeightSet[n + 1] = LtbfMesh[i].weightsets[j][2 + n];
                                num += 1;

                            }
                            else break;

                        }
                        intWeightSet[0] = num;

                        for (int k = 0; k < LtbfMesh[i].weightsets[j][1]; k++)
                        {
                            float[] WeightsetSize = new float[7];
                            if (LtbfMesh[i].weights.Count > 0)
                            {
                                WeightsetSize = LtbfMesh[i].weights[pWeightset];

                                int outw = GetBoneWeightD(intWeightSet, WeightsetSize);
                                LtbfMesh[i].weightsets_output.Add(outw);
                            }
                            else LtbfMesh[i].weightsets_output.Add(LtbfMesh[i].weightsets[j][2]);

                            pWeightset += 1;
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < LtbfMesh[i].nvertices; j++)
                        LtbfMesh[i].weightsets_output.Add(LtbfMesh[i].weightsets[0][0]);
                }
            }
            // weightsets_output
        }

        public static void parse_animation(BinaryReader gbStream)
        {
            int nskipdata = gbStream.ReadInt32();
            long skipzie = 0; ;
            for (int k = 0; k < nskipdata; k++)
            {

                read_string(LTBFile);
                skipzie = gbStream.ReadUInt32();
                gbStream.BaseStream.Position += skipzie * 4;
            }

            UInt16 CompAnim = gbStream.ReadUInt16();
            UInt32 CompAnim2 = gbStream.ReadUInt16();
     
            numAnim = gbStream.ReadUInt16();
            LtbfAnim = new AnimData[numAnim];
             gbStream.ReadUInt16();

            for (int i = 0; i < numAnim; i++)
            {
                // LtbfAnim[i].listkeyframe.Clear();
                LtbfAnim[i].Dim = new float[3];
                LtbfAnim[i].listkeyframe = new List<int>();
                LtbfAnim[i].listsound = new List<string>();
                LtbfAnim[i].frame = new framedata[numBones];
                LtbfAnim[i].Dim[0] = gbStream.ReadSingle();
                LtbfAnim[i].Dim[1] = gbStream.ReadSingle();
                LtbfAnim[i].Dim[2] = gbStream.ReadSingle();
                LtbfAnim[i].name = read_string(gbStream);
                gbStream.ReadUInt32();
                LtbfAnim[i].interp_time = (int)gbStream.ReadUInt32();
                LtbfAnim[i].nkeyframe = gbStream.ReadUInt32();
                for (int j = 0; j < LtbfAnim[i].nkeyframe; j++)
                {
                    LtbfAnim[i].listkeyframe.Add((int)gbStream.ReadUInt32());
                    LtbfAnim[i].listsound.Add(read_string(gbStream));

                }

                int nsup = gbStream.ReadInt16();
                Boolean first = false;
               // if (nsup != 0) MessageBox.Show(nsup+"");
                for (int k = 0; k < numBones; k++)
                {
                    LtbfAnim[i].frame[k].pos = new List<float[]>();
                    LtbfAnim[i].frame[k].quats = new List<float[]>();

                    if (nsup != 0)
                    {
                        if (first == false)
                        {
                            first = true;
                            gbStream.BaseStream.Position -= 2;
                        }

                        int gframe_2;
                        int gframe_1;
                        gframe_1 = gbStream.ReadInt16();

                        gbStream.ReadInt16();
                        float[] p = new float[3];
                        float[] q = new float[4];
                        for (int j = 0; j < gframe_1; j++)
                        {
                            p = new float[3];
                            p[0] = UnpackFromInt16(gbStream.ReadInt16());
                            p[1] = UnpackFromInt16(gbStream.ReadInt16());
                            p[2] = UnpackFromInt16(gbStream.ReadInt16());
                            LtbfAnim[i].frame[k].pos.Add(p);

                        }
                        if (gframe_1 < LtbfAnim[i].nkeyframe)
                            for (int j = gframe_1; j < LtbfAnim[i].nkeyframe; j++)
                            {
                                LtbfAnim[i].frame[k].pos.Add(p);
                            }
                        gframe_2 = gbStream.ReadInt16();
                        gbStream.ReadInt16();
                        for (int j = 0; j < gframe_2; j++)
                        {
                            q = new float[4];
                            q[0] = gbStream.ReadUInt16() / 32767.0f;
                            q[1] = gbStream.ReadUInt16() / 32767.0f;
                            q[2] = gbStream.ReadUInt16() / 32767.0f;
                            q[3] = gbStream.ReadUInt16() / 32767.0f;
                            LtbfAnim[i].frame[k].quats.Add(q);
                        }
                        if (gframe_2 < LtbfAnim[i].nkeyframe)
                            for (int j = gframe_2; j < LtbfAnim[i].nkeyframe; j++)
                            {
                                LtbfAnim[i].frame[k].quats.Add(q);
                            }

                    }
                    else
                    {

                        if (k >= 2)
                        {
                            gbStream.ReadByte();
                        }

                        for (int j = 0; j < LtbfAnim[i].nkeyframe; j++)
                        {

                            float[] p = new float[3];
                            if (k == 0)
                            {

                                p[0] = UnpackFromInt16((Int16)LTBFile.ReadUInt16());
                                LTBFile.ReadUInt16();
                                p[1] = UnpackFromInt16((Int16)LTBFile.ReadUInt16());
                                LTBFile.ReadUInt16();
                                p[2] = UnpackFromInt16((Int16)LTBFile.ReadUInt16());
                                LTBFile.ReadUInt16();


                            }
                            else
                            {

                                p[0] = LTBFile.ReadSingle();
                                p[1] = LTBFile.ReadSingle();
                                p[2] = LTBFile.ReadSingle();

                            }

                            LtbfAnim[i].frame[k].pos.Add(p);

                        }
                        for (int j = 0; j < LtbfAnim[i].nkeyframe; j++)
                        {
                            float[] q = new float[4];
                            if (k == 0)
                            {
                                q[0] = (float)LTBFile.ReadUInt16() / 32767.0f;
                                LTBFile.ReadInt16();
                                q[1] = (float)LTBFile.ReadUInt16() / 32767.0f;
                                LTBFile.ReadInt16();
                                q[2] = (float)LTBFile.ReadUInt16() / 32767.0f;
                                LTBFile.ReadInt16();
                                q[3] = (float)LTBFile.ReadUInt16() / 32767.0f;
                                LTBFile.ReadInt16();
                            }
                            else
                            {
                                q[0] = LTBFile.ReadSingle();
                                q[1] = LTBFile.ReadSingle();
                                q[2] = LTBFile.ReadSingle();
                                q[3] = LTBFile.ReadSingle();
                            }
                            LtbfAnim[i].frame[k].quats.Add(q);
                         
                        }
                    }
                }
            }


        }

        public static void parse_skeleton(BinaryReader gbStream)
        {
            for (int n = 0; n < numBones; n++)
            {
                LtbfBone[n].matdata = new double[4, 4];
                LtbfBone[n].name = read_string(gbStream);
                LtbfBone[n].isbone = gbStream.ReadByte();
                LtbfBone[n].num2 = LTBFile.ReadUInt16();

                for (long i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        LtbfBone[n].matdata[i, j] = LTBFile.ReadSingle();
                    }
                }
                LtbfBone[n].nSubbone = LTBFile.ReadUInt32();
            }
        }
        public static uint totalmesh = 0;
        public static void parse_mesh(BinaryReader gbStream, uint numMesh)
        {
            for (int i = 0; i < numMesh; i++)
            {
                string meshName = read_string(gbStream);
              
                uint numSubmesh = gbStream.ReadUInt32();
                for (int j = 0; j < numSubmesh; j++)
                    gbStream.ReadSingle();
                gbStream.ReadUInt32(); gbStream.ReadUInt32();
                parse_submesh(gbStream, numSubmesh, i, meshName);
            }
        }
        public static void parse_submesh(BinaryReader gbStream, uint numSubmesh, int imesh, string meshName)
        {
            for (int i = 0; i < numSubmesh; i++)
            {
                imesh = (int)totalmesh;


                LtbfMesh[imesh].name = meshName + "_" + i;
                LtbfMesh[imesh].uvs = new List<float[]>();
                LtbfMesh[imesh].normals = new List<float[]>();
                LtbfMesh[imesh].vertices = new List<float[]>();
                LtbfMesh[imesh].weights = new List<float[]>();
                LtbfMesh[imesh].triangles = new List<int>();
                LtbfMesh[imesh].weightsets = new List<int[]>();
                LtbfMesh[imesh].weightsets_output = new List<int>();
                gbStream.ReadUInt32();
                uint matNum = gbStream.ReadUInt32();
                gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32();
                gbStream.ReadByte();
                uint unk1 = gbStream.ReadUInt32();
                uint sectionSize = gbStream.ReadUInt32();

                if (sectionSize != 0)
                {
                    long start = gbStream.BaseStream.Position;
                    uint numVerts = gbStream.ReadUInt32();
                    uint numIdx = gbStream.ReadUInt32() * 3;
                    uint meshType = gbStream.ReadUInt32();
                    if (meshType > 20)
                    {
                        is__doing = false;
                        return;
                    }
                    LtbfMesh[imesh].nvertices = numVerts;
                    LtbfMesh[imesh].type = meshType;
                    LtbfMesh[imesh].nIdx = numIdx;
                    gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32();
                    uint a = 0;
                    if (unk1 == 4)
                    {
                        a = gbStream.ReadUInt32();
                        LtbfMesh[imesh].weightsets.Add(new int[1] { (int)a });
                    }
                    if (unk1 == 5)
                        a = gbStream.ReadUInt16();
                    parse_vertices(gbStream, numVerts, meshType, imesh);
                    for (int j = 0; j < numIdx; j++)
                    {
                        LtbfMesh[imesh].triangles.Add(LTBFile.ReadUInt16());
                    }
                    if (unk1 == 5)
                    {
                        int numWeight = gbStream.ReadInt32();
                        for (long j = 0; j < numWeight; j++)
                        {
                            LtbfMesh[imesh].weightsets.Add(new int[7] { gbStream.ReadInt16(), gbStream.ReadInt16(), gbStream.ReadSByte(), gbStream.ReadSByte(), gbStream.ReadSByte(), gbStream.ReadSByte(), gbStream.ReadInt32() });
                        }
                    }
                    // gbStream.BaseStream.Position = ispos+remain;
                    long unk2 = gbStream.ReadByte();
                    gbStream.BaseStream.Position += unk2;
                }
                totalmesh += 1;
            }
        }
        public static void parse_vertices(BinaryReader gbStream, uint numVerts, uint meshType, int imesh)
        {
           
            if (meshType == 3)
            {
                meshType = LTB_MESHTYPE_TWOEXTRAFLOAT;
            }
            Boolean IncludeWeights = false;
            uint SkipDataSize = 0;
            if (meshType == LTB_MESHTYPE_NOTSKINNED)
            {
                IncludeWeights = false;
            }
            else
                if (meshType == LTB_MESHTYPE_EXTRAFLOAT)
                {
                    IncludeWeights = true;
                    //SkipDataSize = sizeof(Single);
                }
                else
                    if ((meshType == LTB_MESHTYPE_SKINNED) ||
                        (meshType == LTB_MESHTYPE_SKINNEDALT))
                    {
                        IncludeWeights = true;
                    }
                    else
                        if (meshType == LTB_MESHTYPE_TWOEXTRAFLOAT)
                        {
                            IncludeWeights = true;
                        }
            for (int i = 0; i < numVerts; i++)
            {
                LtbfMesh[imesh].vertices.Add(new float[3] { gbStream.ReadSingle(), gbStream.ReadSingle(), gbStream.ReadSingle() });

                if (IncludeWeights)
                {

                    float f1; f1 = gbStream.ReadSingle();
                    float f2 = 0.0f;
                   
                    if (meshType != LTB_MESHTYPE_EXTRAFLOAT)
                    f2 = gbStream.ReadSingle();
                    else f2 = 1.0f - f1;
                   
                    float f3 = 0.0f;
                    float f4 = 0.0f;
                    if (meshType != LTB_MESHTYPE_TWOEXTRAFLOAT && meshType != LTB_MESHTYPE_EXTRAFLOAT) 
                    f3 = gbStream.ReadSingle();
                    else f3 = 1.0f - (f2 + f1);
                    f4 = 1.0f - (f1 + f2 + f3);
                    if (f4 < 0.0f) f4 = 0.0f;
                    LtbfMesh[imesh].weights.Add(new float[4] { f1, f2, f3, f4 });

                }
                gbStream.BaseStream.Position = gbStream.BaseStream.Position + (int)SkipDataSize;
                LtbfMesh[imesh].normals.Add(new float[3] { gbStream.ReadSingle(), gbStream.ReadSingle(), gbStream.ReadSingle() });
                float[] uv = new float[2] { gbStream.ReadSingle(), 1.0f - gbStream.ReadSingle() };
                if (uv[0] > 1.0f) uv[0] -= 1.0f;
                LtbfMesh[imesh].uvs.Add(uv);
            }
        }
        public static double[,] worldToLocalMatrix(double[,] Matrix4x4, int parentIndex, List<double[,]> hMas)
        {
            double[,] localMatrix = new double[4, 4];
            double[,] UnitMatrix = new double[4, 4];
            double[,] inverseMatrix = new double[4, 8];
            if (parentIndex < 0) return Matrix4x4;
      
            int index = parentIndex;
            /*
            for (int i = 0; i < 4; i++)
            {
                double[] roots = solveEquations(new double[4, 5] { { hMas[index][0, 0], hMas[index][1, 0], hMas[index][2, 0], hMas[index][3, 0], UnitMatrix[i, 0] }, { hMas[index][0, 1], hMas[index][1, 1], hMas[index][2, 1], hMas[index][3, 1], UnitMatrix[i, 1] }, { hMas[index][0, 2], hMas[index][1, 2], hMas[index][2, 2], hMas[index][3, 2], UnitMatrix[i, 2] }, { hMas[index][0, 3], hMas[index][1, 3], hMas[index][2, 3], hMas[index][3, 3], UnitMatrix[i, 3] } });
                for (int j = 0; j < 4; j++)
                {
                    inverseMatrix[i, j] = roots[j];
                }
            }
            */
            inverseMatrix = InverseMat2(hMas[index]);
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    localMatrix[i, j] = inverseMatrix[i, 0] * Matrix4x4[0, j] + inverseMatrix[i, 1] * Matrix4x4[1, j] + inverseMatrix[i, 2] * Matrix4x4[2, j] + inverseMatrix[i, 3] * Matrix4x4[3, j];
                }
            }
            return localMatrix;
        }
        public static double[] GetPosition(double[,] matrix)
        {
            var x = matrix[0, 3];
            var y = matrix[1, 3];
            var z = matrix[2, 3];
            return new double[] { x, y, z };
        }
       
        public static double[] GetRotation(double[,] m)
        {
            
            double tr = m[0, 0] + m[1, 1] + m[2, 2];
            double x, y, z, w;
            if (tr > 0f)
            {
                double s = Math.Sqrt(1f + tr) * 2f;
                w = 0.25f * s;
                x = (m[2, 1] - m[1, 2]) / s;
                y = (m[0, 2] - m[2, 0]) / s;
                z = (m[1, 0] - m[0, 1]) / s;
            }
            else if ((m[0, 0] > m[1, 1]) && (m[0, 0] > m[2, 2]))
            {
                double s = Math.Sqrt(1f + m[0, 0] - m[1, 1] - m[2, 2]) * 2f;
                w = (m[2, 1] - m[1, 2]) / s;
                x = 0.25f * s;
                y = (m[0, 1] + m[1, 0]) / s;
                z = (m[0, 2] + m[2, 0]) / s;
            }
            else if (m[1, 1] > m[2, 2])
            {
                double s = Math.Sqrt(1f + m[1, 1] - m[0, 0] - m[2, 2]) * 2f;
                w = (m[0, 2] - m[2, 0]) / s;
                x = (m[0, 1] + m[1, 0]) / s;
                y = 0.25f * s;
                z = (m[1, 2] + m[2, 1]) / s;
            }
            else
            {
                double s = Math.Sqrt(1f + m[2, 2] - m[0, 0] - m[1, 1]) * 2f;
                w = (m[1, 0] - m[0, 1]) / s;
                x = (m[0, 2] + m[2, 0]) / s;
                y = (m[1, 2] + m[2, 1]) / s;
                z = 0.25f * s;
            }

            double[] quat = new double[] { x, y, z, w };
            return quat;
        }
        public static float[] GetScale(float[,] m)
        {
            var x = (float)Math.Sqrt(m[0, 0] * m[0, 0] + m[0, 1] * m[0, 1] + m[0, 2] * m[0, 2]);
            var y = (float)Math.Sqrt(m[1, 0] * m[1, 0] + m[1, 1] * m[1, 1] + m[1, 2] * m[1, 2]);
            var z = (float)Math.Sqrt(m[2, 0] * m[2, 0] + m[2, 1] * m[2, 1] + m[2, 2] * m[2, 2]);

            return new float[] { x, y, z };
        }
        public static double[] quaternionToRotation(double[] quaternion)
        {
            
            double norm =Math.Sqrt(quaternion[0] * quaternion[0] + quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2] + quaternion[3] * quaternion[3]);
          
            if (norm > 1.0)
            {
                quaternion[0] /= norm;
                quaternion[1] /= norm;
                quaternion[2] /= norm;
                quaternion[3] /= norm;
               
            }
        
            
            double[] rotation = new double[3];
            rotation[0] = Math.Atan2(2 * (quaternion[3] * quaternion[0] + quaternion[1] * quaternion[2]), 1 - 2 * (quaternion[0] * quaternion[0] + quaternion[1] * quaternion[1]));
            
            rotation[1] = Math.Asin(2 *(quaternion[3] * quaternion[1] - quaternion[2] * quaternion[0]));
            rotation[2] = Math.Atan2(2 * (quaternion[3] * quaternion[2] + quaternion[0] * quaternion[1]), 1 - 2 * (quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2]));
          

 
            return rotation;

        }
        public static double[] quaternionToRotation2(double[] quaternion)
        {

            double norm = Math.Sqrt(quaternion[0] * quaternion[0] + quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2] + quaternion[3] * quaternion[3]);

            double[] rotation = new double[3];
            
            if (norm > 1.0)
            {
                quaternion[0] /= norm;
                quaternion[1] /= norm;
                quaternion[2] /= norm;
                //quaternion[3] /= norm;

            }
            

           
            rotation[0] = Math.Atan2(2 * (quaternion[3] * quaternion[0] + quaternion[1] * quaternion[2]), 1 - 2 * (quaternion[0] * quaternion[0] + quaternion[1] * quaternion[1]));

            rotation[1] = Math.Asin(2 * (quaternion[3] * quaternion[1] - quaternion[2] * quaternion[0]));
            rotation[2] = Math.Atan2(2 * (quaternion[3] * quaternion[2] + quaternion[0] * quaternion[1]), 1 - 2 * (quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2]));
            rotation [0]= NormalAngle(rotation[0]);
            rotation[1] = NormalAngle(rotation[1]);
            rotation[2] = NormalAngle(rotation[2]);
            return rotation;

        }
        public static double[] quaternionToRotation3(double[] q1)
        {
            double sqw = q1[3] * q1[3];
            double sqx = q1[0] * q1[0];
            double sqy = q1[1] * q1[1];
            double sqz = q1[2] * q1[2];
            double unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            double test = q1[0] * q1[2] + q1[2] * q1[3];
            double heading, attitude, bank;
            if (test > 0.499 * unit)
            { // singularity at north pole
                heading = 2 * Math.Atan2(q1[0], q1[3]);
                attitude = Math.PI / 2;
                bank = 0;

                
                return new double[] { heading, attitude, bank }; 
            }
            if (test < -0.499 * unit)
            { // singularity at south pole
                heading = -2 * Math.Atan2(q1[0], q1[3]);
                attitude = -Math.PI / 2;
                bank = 0;

                return new double[] { heading, attitude, bank };
            }
            heading = Math.Atan2(2 * q1[1] * q1[3] - 2 * q1[0] * q1[2], sqx - sqy - sqz + sqw);
            attitude = Math.Asin(2 * test / unit);
            bank = Math.Atan2(2 * q1[0] * q1[3] - 2 * q1[1] * q1[2], -sqx + sqy - sqz + sqw);

            return new double[] { heading, attitude, bank };

        }
        public static double NormalAngle( double angle)
        {
            while (angle > Math.PI * 2) angle -= Math.PI * 2;
            while (angle < 0) angle += Math.PI * 2;
            return angle;

        }
        public static double[] QuaternionToYawPitchRoll(double[] q)
        {

            const double Epsilon = 0.0009765625f;
            const double Threshold = 0.5f - Epsilon;

            double yaw;
            double pitch;
            double roll;

            double XY = q[0] * q[1];
            double ZW = q[2] * q[3];

            double TEST = XY + ZW;

            if (TEST < -Threshold || TEST > Threshold)
            {

                int sign = Math.Sign(TEST);

                yaw = sign * 2 * (double)Math.Atan2(q[0], q[3]);

                pitch = sign * Math.PI / 2.0;

                roll = 0;

            }
            else
            {

                double XX = q[0] * q[0];
                double XZ = q[0] * q[2];
                double XW = q[0] * q[3];

                double YY = q[1] * q[1];
                double YW = q[1] * q[3];
                double YZ = q[1] * q[2];

                double ZZ = q[2] * q[2];

                yaw = (double)Math.Atan2(2 * YW - 2 * XZ, 1 - 2 * YY - 2 * ZZ);

                pitch = (double)Math.Atan2(2 * XW - 2 * YZ, 1 - 2 * XX - 2 * ZZ);

                roll = (double)Math.Asin(2 * TEST);

            }//if 

            return new double[]{yaw, pitch, roll};

        }//method 







        public static double[,] savequot;
        public static double[] constquot;
        public static double[] saveResult;
        public static double[] solveEquations(double[,] quot)
        {
            int count = quot.GetLength(0);
            savequot = new double[count, count];
            constquot = new double[count];
            saveResult = new double[count];
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    savequot[i, j] = quot[i, j];
                }
                constquot[i] = quot[i, count];
                saveResult[i] = 0;
            }

            double basic = 0;
            basic = getMatrixResult(savequot);
            if (Math.Abs(basic) < 0.00001)
            {
                return saveResult;
            }
            double[,] temp = new double[saveResult.GetLength(0), saveResult.GetLength(0)];
            for (int i = 0; i < saveResult.GetLength(0); i++)
            {
                temp = getReplaceMatrix(i);
                saveResult[i] = getMatrixResult(temp) / basic;
            }
            return saveResult;
        }
        public static double getMatrixResult(double[,] input)
        {
            if (input.GetLength(0) == 2)
            {
                return input[0, 0] * input[1, 1] - input[0, 1] * input[1, 0];
            }
            else
            {
                double[] temp = new double[input.GetLength(0)];
                double[,] tempinput = new double[input.GetLength(0) - 1, input.GetLength(0) - 1];
                double result = 0;
                for (int i = 0; i < input.GetLength(0); i++)
                {
                    temp[i] = input[i, 0];
                    int m = 0, n = 0;
                    for (int k = 0; k < input.GetLength(0); k++)
                    {
                        if (k != i)
                        {
                            for (m = 0; m < input.GetLength(0) - 1; m++)
                            {
                                tempinput[n, m] = input[k, m + 1];
                            }
                            n++;
                        }
                    }
                    if (i % 2 == 0)
                    {
                        result = result + temp[i] * getMatrixResult(tempinput);
                    }
                    else
                    {
                        result = result - temp[i] * getMatrixResult(tempinput);
                    }
                }
                return result;
            }
        }
        public static double[,] getReplaceMatrix(int i)
        {
            double[,] tempresult = new double[savequot.GetLength(0), savequot.GetLength(0)];
            for (int m = 0; m < savequot.GetLength(0); m++)
            {
                for (int n = 0; n < savequot.GetLength(0); n++)
                {
                    if (i != m)
                    {
                        tempresult[n, m] = savequot[n, m];
                    }
                    else
                    {
                        tempresult[n, i] = constquot[n];
                    }
                }
            }
            return tempresult;
        }

        private void button2_Click()
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                input_ltb.Clear();
                string Text_box = "";
                foreach (String file in openFileDialog2.FileNames)
                {
                    int pos_ext = file.LastIndexOf(".");
                    input_ltb.Add(file);
                    if (Text_box.Length > 0) Text_box += "\n" + file;
                    else Text_box += file;
                }
                label2.Text = Text_box;
              //  button3.Enabled = true;
            }
        }

        private void button3_Click()
        {
          //  button3.Enabled = false;
           // button2.Enabled = false;
            if (is__doing == true || input_ltb.Count <= 0) return;
            numAnim = 0;
            numBones = 0;
            numMesh = 0;
            Scaleto = Convert.ToInt32(textBox1.Text);
            Stopwatch st = new Stopwatch();
            st.Start();

            if (input_ltb.Count > 0)
            {
                for (int i = 0; i < input_ltb.Count; i++)
                {
                    __doing_convert(input_ltb[i]);
                   
                }
            }
            is__doing = false;
            st.Stop();
             richTextBox1.AppendText("[-] Tổng thời gian của quá trình:"+((float)st.ElapsedMilliseconds/1000.0f).ToString("F4")+"s \n"); 
                    
           // button3.Enabled = true;
          //  button2.Enabled = true;
        }
       
        private void __doing_convert(string File_in)
        {
            totalmesh = 0;
            is__doing = true;
            richTextBox1.AppendText("\n");
            int pos_ext = File_in.LastIndexOf(".");
            int pos_Path = File_in.LastIndexOf("\\");
            string fname = File_in.Substring(pos_Path + 1, pos_ext - pos_Path - 1);
            string gPath = File_in.Substring(0, pos_Path + 1);

            cur_fName = fname;
            cur_Path = gPath;
            richTextBox1.AppendText("[+] Path:" + gPath + "\n");
            richTextBox1.AppendText("[+] file:" + fname + ".ltb\n");

            FileStream fileStream = new FileStream(File_in, FileMode.Open);
            LTBFile = new BinaryReader(fileStream);
            richTextBox1.AppendText("[+] Lấy dữ liệu trong file:" + fname + ".ltb\n");

           
            int Check_header=LTBFile.ReadUInt16(); LTBFile.ReadUInt16();
            LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32();
            
            richTextBox1.AppendText("[+] Kiểm tra kiểu file\n");
            Boolean is_tmp = false;
            if (Check_header > 20)
            {
                LTBFile.Close();
                richTextBox1.AppendText("   [!] File pack lmza\n");
                richTextBox1.AppendText("   - Tiến hành decompress\n");
                Decompress_file(File_in, "___tmp.tmp");
                is_tmp = true;
                richTextBox1.AppendText("[+] Tiến hành đọc lại file\n");
                fileStream = new FileStream("___tmp.tmp", FileMode.Open);
                LTBFile = new BinaryReader(fileStream);
             
                LTBFile.ReadUInt16(); LTBFile.ReadUInt16();
                LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32();

            }
            UInt32 version = LTBFile.ReadUInt32();
           
            LTBFile.ReadUInt32(); LTBFile.ReadUInt32();
            numBones = LTBFile.ReadUInt32();
            LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32(); LTBFile.ReadUInt32();
            LTBFile.ReadUInt16(); LTBFile.ReadUInt16();
            LTBFile.ReadUInt32();
            LTBFile.ReadBytes(LTBFile.ReadUInt16());
            LTBFile.ReadSingle();
            LTBFile.ReadUInt32();
            numMesh = LTBFile.ReadUInt32();
            richTextBox1.AppendText("     - File version:" + version + "\n");
            richTextBox1.AppendText("     - Khởi tạo biến lưu trữ\n");

            LtbfMesh = new MeshData[256];
            LtbfBone = new BoneData[numBones];
            richTextBox1.AppendText("     - Lấy dữ liệu cơ sở\n");
            parse_mesh(LTBFile, numMesh);
            numMesh = totalmesh;
            if (is__doing == false)
            {
                richTextBox1.AppendText("     - Quá trình thất bại [Chư hỗ trợ kiểu file này] \n");
                LTBFile.Close();
                return;
            }
            Calc_weightsets();
            parse_skeleton(LTBFile);
            richTextBox1.AppendText("     - Tính toán cơ sở\n");
            Clac_Par_Bone();
            
            if (isCheckboxAnim == true)
            {
                richTextBox1.AppendText("     - Kiểm tra anim\n");
                if (LTBFile.BaseStream.Length - LTBFile.BaseStream.Position < 2048) isAnim = false;
                else isAnim = true;
                //  isAnim = false;
                if (isAnim == true)
                {
                    richTextBox1.AppendText("           + Lấy dữ liệu về anim\n");
                    parse_animation(LTBFile);
                    richTextBox1.AppendText("               * Có "+numAnim +" anim\n");
                }
                else
                    richTextBox1.AppendText("           + Không lấy được anim trong file này\n");
            }
            else isAnim = false;

            richTextBox1.AppendText("[+] Ghi mesh vào file:" + fname + ".smd\n");
          //  Scale_();
            calc_databone();
          //  get_new_bone_out_data(0, 0, 1.0f,1.0f,0.65f);
         //   Change_a_anim(1.0f, 1.0f, 0.65f);
            Write_SMD_MODEL(gPath+fname);
            if (isAnim == true)
                for (int i = 0; i < numAnim; i++)
                {
                    richTextBox1.AppendText("[+] Ghi Anim " + LtbfAnim[i].name + " vào file:" + LtbfAnim[i].name + ".smd\n");
                    Write_SMD_ANIM(i, gPath+ LtbfAnim[i].name + ".smd");
                }
             if (isAutoCreateQC == true && isAnim == true)
            {
                richTextBox1.AppendText("[+] Tạo file QC file:" + fname + ".qc\n");
                Write_QC(gPath + fname + ".qc", fname);
            }

            richTextBox1.AppendText("Hoàn thành!! Bạn có thể kéo lên để xem lại thông báo của quá trình\n");
            LTBFile.Close();
            if (is_tmp == true) File.Delete("___tmp.tmp");

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (isSubForm == false)
            {
                isSubForm = true;
                Form1.ActiveForm.Height += 45;
                button1.Text = "-";
                isAutoScaler = true;

            }
            else
            { isAutoScaler = true;
                isSubForm = false;
                Form1.ActiveForm.Height -= 45;
                button1.Text = "+";
                isAutoScaler = false;
            }
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (isCalcframe == true) isCalcframe = false;
            else isCalcframe = true;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (isAutoCreateQC == false) isAutoCreateQC = true;
            else isAutoCreateQC = false;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (isCheckboxAnim == false) isCheckboxAnim = true;
            else isCheckboxAnim = false;
            
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (is_spl_model == false) is_spl_model = true;
            else is_spl_model = false;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (is_slp_hand == false) is_slp_hand = true;
            else is_slp_hand = false;
        }
        static bool Decompress_file(string inFile, string outFile)
        {
            var input = new FileStream(inFile, FileMode.Open);
            var decoder = new LzmaDecodeStream(input);
            
            try
            {
                var output = new FileStream(outFile, FileMode.Create);

                int bufSize = 24576, count;
                byte[] buf = new byte[bufSize];
                while ((count = decoder.Read(buf, 0, bufSize)) > 0)
                {
                    output.Write(buf, 0, count);
                }
                input.Close();
                output.Close();
            }
            catch
            {
                return false;
            }
            return true;

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            button2_Click();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            button3_Click();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
          //  DtxRead
            write_dll();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
               
               
                foreach (String file in openFileDialog1.FileNames)
                {
                     richTextBox1.AppendText("\n");
                    int pos_ext = file.LastIndexOf(".");
                    int pos_Path = file.LastIndexOf("\\");
                    string fname = file.Substring(pos_Path + 1, pos_ext - pos_Path - 1);
                    string gPath = file.Substring(0, pos_Path + 1);

                    richTextBox1.AppendText("\n [+] Convert DTX:" + fname + ".dtx \n");
                    DtxRead.LoadFile(file);
                    DtxRead.Save_to("v");
                    File.Delete(gPath + fname + ".bmp");
                    File.Move("v", gPath + fname + ".bmp");
                  
                }
              
                
                //  button3.Enabled = true;
            }
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            bool[] old = new bool[] { isAutoScaler, isSubForm, isCalcframe, isAutoCreateQC, isCheckboxAnim };

              
            isAutoScaler = true;
            isSubForm = true;
            isCalcframe = true;
            isAutoCreateQC = true;
            isCheckboxAnim = true;
            button3_Click();
       
       
            if (numAnim > 0)
            {


                write_mdllib(cur_Path);
             
                for (int i = 0; i < numMesh;i++ )
                    if (!File.Exists(cur_Path + LtbfMesh[i].name.Replace(" ", "_") + ".bmp"))
                
                    File.WriteAllBytes(cur_Path+LtbfMesh[i].name.Replace(" ", "_") + ".bmp", Properties.Resources.textures);
               
                richTextBox1.AppendText("\n [+]Run studiomdl.exe \n");
                File.Delete(cur_Path + cur_fName + ".mdl");
                var process = new Process();

                process.StartInfo.FileName = cur_Path+"studiomdl.exe";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.WorkingDirectory = cur_Path;
                
                process.StartInfo.Verb = "runas";
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                richTextBox1.AppendText("     - Compile: "+cur_Path + cur_fName + ".qc \n");
                process.StartInfo.Arguments =  cur_fName + ".qc";
                process.Start();
                process.WaitForExit();
                process.Close();
               
               // for (int i = 0; i < numMesh; i++)
                //    File.Delete(cur_Path + LtbfMesh[i].name.Replace(" ", "_") + ".bmp \n");
                if (!File.Exists(cur_Path + cur_fName + ".mdl")) richTextBox1.AppendText("     /!\\ Có lỗi xảy ra không thấy output ");
                else richTextBox1.AppendText("     [y] Hoàn thành!!!\n ");
                
            }
            isAutoScaler = old[0];
            isSubForm = old[1];
            isCalcframe = old[2];
            isAutoCreateQC = old[3];
            isCheckboxAnim = old[4];
          
        }

    }
    class DtxRead
    {
        private static BinaryReader fFile_load;
        private static int iWidth, iHeight, iBpp, iSize;
        private static SquishFlags pInternalFormat;
        public static byte[] imageData;
        public static bool LoadFile(String File_in)
        {
            FileStream fileStream = new FileStream(File_in, FileMode.Open);
            fFile_load = new BinaryReader(fileStream);
            bool isTMP = false;
            uint iResType = fFile_load.ReadUInt32();
            if (iResType > 20)
            {
                fFile_load.Close();
                Decompress_file(File_in, "___tmp.tmp");
                fileStream = new FileStream("___tmp.tmp", FileMode.Open);
                fFile_load = new BinaryReader(fileStream);
                 iResType = fFile_load.ReadUInt32();
                 isTMP = true;
             
            }

            int iVersion = fFile_load.ReadInt32();
            ushort usWidth = fFile_load.ReadUInt16();
            ushort usHeight = fFile_load.ReadUInt16();
            ushort usMipmaps = fFile_load.ReadUInt16();
            ushort usSections = fFile_load.ReadUInt16();
            int iFlags = fFile_load.ReadInt32();
            int iUserFlags = fFile_load.ReadInt32();
            byte[] ubExtra = fFile_load.ReadBytes(12);
            byte[] szCommandString = fFile_load.ReadBytes(128);

            if (iResType != 0 || iVersion != -5 || usMipmaps == 0)
            {
                fFile_load.Close();
                if (isTMP == true) File.Delete("___tmp.tmp");
                return false;
            }
            iWidth = (int)usWidth;
            iHeight = (int)usHeight;
            iBpp = (int)ubExtra[2];

            if (iBpp == 3)
            {
                iSize = usWidth * usHeight * 4;
                pInternalFormat = SquishFlags.Dxt1;
            }
            else if (iBpp == 4)
            {
                iSize = (usWidth * usHeight) >> 1;
                pInternalFormat = SquishFlags.Dxt1;
            }
            else if (iBpp == 5)
            {
                iSize = usWidth * usHeight;
                pInternalFormat = SquishFlags.Dxt3;
            }
            else if (iBpp == 6)
            {
                iSize = usWidth * usHeight;
                pInternalFormat = SquishFlags.Dxt5;
            }
            else
            {
                iSize = 0;
            }
            if (iSize == 0)
            {
                fFile_load.Close();
                if (isTMP == true) File.Delete("___tmp.tmp");
                return false;
            }
            imageData = new byte[1024 * 1024 * 4];
            byte[] pBuffer = fFile_load.ReadBytes(iSize);
            Console.WriteLine(iBpp);
            if (iBpp == 3)
            {
                imageData = pBuffer;
            }
            else
            {

                imageData = Squish.DecompressImage(pBuffer, iWidth, iHeight, pInternalFormat);
                for (int i = 0; i < iWidth * iHeight; i += 1)
                {
                    byte tmp = imageData[i * 4];
                    imageData[i * 4] = imageData[i * 4 + 2];
                    imageData[i * 4 + 2] = tmp;
                }
            }

            fFile_load.Close();
            if (isTMP==true) File.Delete("___tmp.tmp");
            return true;
        }
        public static void Save_to(String outFile)
        {

            FIBITMAP dib = FreeImage.ConvertFromRawBits(imageData, iWidth, iHeight, 4 * iWidth, 32, FreeImage.FI_RGBA_RED_MASK, FreeImage.FI_RGBA_GREEN_MASK, FreeImage.FI_RGBA_BLUE_MASK, false);

            FreeImage.FlipVertical(dib);
            RGBQUAD[] rev = new RGBQUAD[0];
            FIBITMAP temp = FreeImage.ConvertTo24Bits(dib);
            uint w = FreeImage.GetWidth(temp);
            uint h = FreeImage.GetWidth(temp);
            if (w > 512 || h > 512)
                temp = FreeImage.Rescale(temp, 512, 512, FREE_IMAGE_FILTER.FILTER_BOX);

            temp = FreeImage.ColorQuantizeEx(temp, FREE_IMAGE_QUANTIZE.FIQ_WUQUANT, 256, 0, rev);

            FreeImage.SaveEx(temp, outFile, FREE_IMAGE_FORMAT.FIF_BMP, FREE_IMAGE_SAVE_FLAGS.DEFAULT);
            FreeImage.Unload(temp);
            FreeImage.Unload(dib);
        }
        public static bool Decompress_file(string inFile, string outFile)
        {
            var input = new FileStream(inFile, FileMode.Open);
            var decoder = new LzmaDecodeStream(input);

            try
            {
                var output = new FileStream(outFile, FileMode.Create);

                int bufSize = 24576, count;
                byte[] buf = new byte[bufSize];
                while ((count = decoder.Read(buf, 0, bufSize)) > 0)
                {
                    output.Write(buf, 0, count);
                }
                input.Close();
                output.Close();
            }
            catch
            {
                return false;
            }
            return true;

        }
    }


}
