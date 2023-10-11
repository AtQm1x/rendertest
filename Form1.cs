using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using System.IO;

namespace rendertest
{
	public partial class Form1 : Form
	{
		double scale = 1;
		double tgfps = 60;
		bool dofill = false;
		bool dodraw = true;
		bool dorotate = false;
        bool doublebuffer = false;
        readonly Timer timer = new Timer();
		struct tri
		{
			public Vector3[] p;

			public tri(Vector3 p1, Vector3 p2, Vector3 p3)
			{
				p = new Vector3[3];
				p[0] = p1;
				p[1] = p2;
				p[2] = p3;
			}
		} // triangle structure
		struct Mesh
		{
			public List<tri> Tris;

			public bool LoadFromObjectFile(string sFilename)
			{
				using (StreamReader file = new StreamReader(sFilename))
				{
					Tris = new List<tri>();
					if (file == null)
						return false;

					// Local cache of verts
					List<Vector3> verts = new List<Vector3>();

					while (!file.EndOfStream)
					{
						string line = file.ReadLine();
						line = line.Replace('.', ',');
						string[] tokens = line.Split(' ');

						if (tokens.Length < 1)
							continue;

						char firstChar = tokens[0][0];

						if (firstChar == 'v')
						{
							float x, y, z;
							if (float.TryParse(tokens[1], out x) && float.TryParse(tokens[2], out y) && float.TryParse(tokens[3], out z))
							{
								Vector3 v = new Vector3(x, y, z);
								verts.Add(v);
							}
						}

						if (firstChar == 'f')
						{
							int[] f = new int[3];
							if (int.TryParse(tokens[1], out f[0]) && int.TryParse(tokens[2], out f[1]) && int.TryParse(tokens[3], out f[2]))
							{
								Tris.Add(new tri(verts[f[0] - 1], verts[f[1] - 1], verts[f[2] - 1]));
							}
						}
					}
				}
				return true;
			}
		}
		public Form1()
		{
			InitializeComponent();
			timer.Interval = (int)(1000 / tgfps);
			timer.Tick += timer1_Tick;
			timer.Start();
			DoubleBuffered = doublebuffer;

			rotMatX = new Matrix4x4
			{
				M11 = 1f,
				M22 = (float)Math.Cos(angx),
				M23 = -(float)Math.Sin(angx),
				M32 = (float)Math.Sin(angx),
				M33 = (float)Math.Cos(angx)
			};
			rotMatY = new Matrix4x4
			{
				M11 = (float)Math.Cos(angy),
				M13 = (float)Math.Sin(angy),
				M22 = 1f,
				M31 = (float)-Math.Sin(angy),
				M33 = (float)Math.Cos(angy)
			};
			rotMatZ = new Matrix4x4
			{
				M11 = (float)Math.Cos(angz),
				M12 = -(float)Math.Sin(angz),
				M21 = (float)Math.Sin(angz),
				M22 = (float)Math.Cos(angz),
				M33 = 1.0f
			};
			projMat = new Matrix4x4
			{
				M11 = 1f,
				M22 = 1f,
				M44 = 1f
			};

			mesh1.Tris = new List<tri>();
		} // on initialise
		Mesh mesh1 = new Mesh();

		double angx = 0d;
		double angy = 0d;
		double angz = 0d;

		public Matrix4x4 rotMatX, rotMatY, rotMatZ, projMat;
		double FPS = 0;
		DateTime lastFrameTime = DateTime.Now;
		int width, height, centx, centy;
		public void timer1_Tick(object sender, EventArgs e)
        {
            DoubleBuffered = doublebuffer;
            timer.Interval = Math.Abs((int)Math.Round(1000 / (tgfps + 1f)));
			TimeSpan elapsedTime = DateTime.Now - lastFrameTime;
			FPS = 1 / elapsedTime.TotalSeconds;
			lastFrameTime = DateTime.Now;

			if (dorotate)
            {
                angx += 0.015d;
                angy += 0.01d;
                angz += 0.01d;
            }

			rotMatX = new Matrix4x4
			{
				M11 = 1f,
				M22 = (float)Math.Cos(angx),
				M23 = -(float)Math.Sin(angx),
				M32 = (float)Math.Sin(angx),
				M33 = (float)Math.Cos(angx)
			};
			rotMatY = new Matrix4x4
			{
				M11 = (float)Math.Cos(angy),
				M13 = (float)Math.Sin(angy),
				M22 = 1f,
				M31 = (float)-Math.Sin(angy),
				M33 = (float)Math.Cos(angy)
			};
			rotMatZ = new Matrix4x4
			{
				M11 = (float)Math.Cos(angz),
				M12 = -(float)Math.Sin(angz),
				M21 = (float)Math.Sin(angz),
				M22 = (float)Math.Cos(angz),
				M33 = 1.0f
			};
			projMat = new Matrix4x4
			{
				M11 = 1f,
				M22 = 1f,
				M44 = 1f
			};

			width = Width;
			height = Height;
			centx = width / 2;
			centy = height / 2;

			tick += 1;

			Invalidate();
		}
		int tick = 0;
		int polygon_count;
		private void Form1_Paint(object sender, PaintEventArgs e)
		{
			Bitmap buffer = new Bitmap(Width, Height);

			using (Graphics b = Graphics.FromImage(buffer))
			{
				Graphics g = CreateGraphics();
				int num = (int)(125 + (Math.Sin(0.01 * tick) * 125));
				//g.FillRectangle(Brushes.Black, centx - 250, centy - 250, 250, 250);
				List<tri> rastList = new List<tri>();
				g.Clear(Color.Black);

				/* Show FPS, Angles, Controls */
				{
					g.DrawString($"Target FPS: {tgfps}", Font, Brushes.White, 20, 40);
					g.DrawString($"Frame interval: {timer.Interval} ms", Font, Brushes.White, 20, 60);
					g.DrawString($"FPS: {Math.Round(FPS)}", Font, Brushes.White, 20, 80);
					g.DrawString($"Frame number: {tick}", Font, Brushes.White, 20, 100);
					g.DrawString($"Polygon count: {polygon_count}", Font, Brushes.White, 20, 120);
					g.DrawString($"ANG_X: {Math.Round(angx, 2)}", Font, Brushes.White, 20, 140);
					g.DrawString($"ANG_Y: {Math.Round(angy, 2)}", Font, Brushes.White, 20, 160);
					g.DrawString($"ANG_Z: {Math.Round(angz, 2)}", Font, Brushes.White, 20, 180);
					g.DrawString($"'E' / 'Q' To increase / decrease target fps", Font, Brushes.White, 20, 200);
					g.DrawString($"'G' To select an .OBJ file", Font, Brushes.White, 20, 220);
					g.DrawString($"'F' To toggle filling: {dofill}", Font, Brushes.White, 20, 240);
					g.DrawString($"'R' To toggle drawing: {dodraw}", Font, Brushes.White, 20, 260);
					g.DrawString($"'B' To toggle double_buffer: {doublebuffer}", Font, Brushes.White, 20, 280);
					g.DrawString($"'H' To toggle rotation: {dorotate}", Font, Brushes.White, 20, 300);
				}/* Show Stats */

				if (dodraw || dofill)
				{
					polygon_count = 0;
					for (int j = 0; j < mesh1.Tris.Count; j++)
					{
						polygon_count++;
						tri t = mesh1.Tris[j];
						Vector3 normal, line1, line2;
						Vector3[] tproj = new Vector3[3];
						for (int i = 0; i < 3; i++)
						{
							Vector3 pos = t.p[i];
							pos *= (float)scale;
							pos = Vector3.Transform(pos, rotMatX);
							pos = Vector3.Transform(pos, rotMatY);
							pos = Vector3.Transform(pos, rotMatZ);
							tproj[i] = pos;
						}
						line1.X = tproj[1].X - tproj[0].X;
						line1.Y = tproj[1].Y - tproj[0].Y;
						line1.Z = tproj[1].Z - tproj[0].Z;

						line2.X = tproj[2].X - tproj[0].X;
						line2.Y = tproj[2].Y - tproj[0].Y;
						line2.Z = tproj[2].Z - tproj[0].Z;

						normal.X = line1.Y * line2.Z - line1.Z * line2.Y;
						normal.Y = line1.Z * line2.X - line1.X * line2.Z;
						normal.Z = line1.X * line2.Y - line1.Y * line2.X;

						float l = (float)Math.Sqrt((double)(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z));
						normal.X /= l;
						normal.Y /= l;
						normal.Z /= l;

						if (normal.Z > 0)
						{
							for (int i = 0; i < 3; i++)
							{
								tproj[i] = Vector3.Transform(tproj[i], projMat);
							}

							rastList.Add(new tri(tproj[0], tproj[1], tproj[2]));
						}
					}
					rastList.Sort((t1, t2) =>
					{
						float z1 = (t1.p[0].Z + t1.p[1].Z + t1.p[2].Z) / 3.0f;
						float z2 = (t2.p[0].Z + t2.p[1].Z + t2.p[2].Z) / 3.0f;
						return z1 < z2 ? 1 : (z1 > z2 ? -1 : 0);
					});

					foreach (tri t in rastList)
					{
						Point p1 = new Point((int)(t.p[0].X * 100 + centx), (int)(t.p[0].Y * 100 + centy));
						Point p2 = new Point((int)(t.p[1].X * 100 + centx), (int)(t.p[1].Y * 100 + centy));
						Point p3 = new Point((int)(t.p[2].X * 100 + centx), (int)(t.p[2].Y * 100 + centy));
						Point[] p = new Point[3] { p1, p2, p3 };

						//drawing module
						if (dofill)
						{
							g.FillPolygon(Brushes.DarkGray, p);
						}
						if (dodraw)
						{
							g.DrawPolygon(Pens.White, p);
						}
					}
				}
			}
			using (Graphics formGraphics = CreateGraphics())
			{
				//formGraphics.DrawImage(buffer, 0, 0);
			}
			buffer.Dispose();
		}


		public void Form1_KeyDown(object sender, KeyEventArgs e)
		{
			switch (e.KeyCode)
			{
				case Keys.Escape:
					angx = 0;
					angy = 0;
					angz = 0;
					tick = 0;
					break;

				case Keys.E:
					tgfps += 5;
					break;

				case Keys.Q:
					tgfps -= 5;
					break;

				case Keys.W:
					angx -= 0.05;
					break;

				case Keys.S:
					angx += 0.05;
					break;

				case Keys.A:
					angy -= 0.05;
					break;

				case Keys.D:
					angy += 0.05;
					break;

				case Keys.Z:
					angz += 0.05;
					break;

				case Keys.X:
					angz -= 0.05;
					break;

				case Keys.G:
					OpenFileDialog openFileDialog1 = new OpenFileDialog();
					openFileDialog1.InitialDirectory = @"C:\";
					openFileDialog1.Title = "Select an .obj or .txt file that contains the mesh";
					openFileDialog1.Filter = "Wavefront OBJ Files (*.obj)|*.obj|Text Files (*.txt)|*.txt";
					openFileDialog1.FilterIndex = 1; // Set the default filter (Text Files)
					openFileDialog1.RestoreDirectory = true; // Remember the last used 
					if (openFileDialog1.ShowDialog() == DialogResult.OK)
					{
						string filePath = openFileDialog1.FileName;
						mesh1.LoadFromObjectFile(filePath);
					}
					break;

				case Keys.F:
					dofill = !dofill;
					break;

				case Keys.R:
					dodraw = !dodraw;
					break;

				case Keys.C:
					scale += 0.1;
					break;

				case Keys.V:
					scale -= 0.1;
					break;

				case Keys.B:
					doublebuffer = !doublebuffer;
					break;

				case Keys.H:
					dorotate = !dorotate;
					break;



				// Add more cases for other key codes if 
				default:
					// Handle other key codes or do nothing
					break;
			}
		} //Handling user input
	}
}
