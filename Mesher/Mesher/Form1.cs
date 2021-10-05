using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using netDxf;
using netDxf.Blocks;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Header;
using netDxf.Objects;
using netDxf.Tables;
using netDxf.Units;
using Attribute = netDxf.Entities.Attribute;
using FontStyle = netDxf.Tables.FontStyle;
using Image = netDxf.Entities.Image;
using Point = netDxf.Entities.Point;
using Trace = netDxf.Entities.Trace;
using TriangleNet;
using TriangleNet.Geometry;
using TriangleNet.Meshing;
using TriangleNet.Smoothing;
using TriangleNet.Tools;
using System.Drawing.Drawing2D;
using System.Web;
using System.Windows.Forms.VisualStyles;

namespace Mesher
{
    public partial class Form1 : Form
    {

        #region global Parameters

        private MesherDataSet mds = new MesherDataSet();

        bool MeshAnalysisFlag = false;
        bool PlotNormalStress = false;
        bool PlotWarpingFunction = false;
        bool PlotNodeNumber = false;
        bool PlotMesh = false;

        double[] meshNormalStress;
        double[] meshOmega;
        TriangleNet.Mesh mesh;

        double appliedAxialForce = 0.0;
        double appliedMomentX = 10.0;
        double appliedMomentY = 0.0;
        double appliedForceLocationX = 0.0;
        double appliedForceLocationY = 0.0;
        double currentCellValue=0.0;

        Polygon poly;
        List<Vertex> centroids = new List<Vertex>();
        Dictionary<string, double> Results = new Dictionary<string, double>();
        List<string> contents = new List<string>();
        
        #endregion

        public Form1()
        {
            InitializeComponent();


            string[] filePaths = Directory.GetFiles(@"C:\Users\CWang\Documents\Github\dxf2xml\Mesher\Mesher\bin\Debug", "*.dxf", SearchOption.TopDirectoryOnly);
            foreach(string filePath in filePaths)
            {
                string fileName = Path.GetFileName(filePath);
                mds = new MesherDataSet(fileName);
                contents = new List<string>();

                // Write to Three.JS
                Write23JS();
            }
            

            


            // Calculate I, a, z value of the top and bottom frame
            Export();

            // Write to XML
            // Write2XML();
        }

        List<LinearDimension> dims = new List<LinearDimension>();


        public void Write23JS()
        {
            string articleName = "article__" + mds.articleNumber;
            string path1 = Environment.CurrentDirectory + "/" + articleName + ".js";
          
            dxfLayerSearch(mds.fileName);
            using (StreamWriter sw = File.CreateText(path1))
            {
                foreach (var str in contents)
                {
                    sw.WriteLine(str);
                }
            }
        }


        public void Write2XML()
        {
            

            string optime = DateTime.Now.ToString("yyyyMMddHHmmss") + ".xml";
            System.Data.DataSet ds = new System.Data.DataSet("Article");

            System.Data.DataTable table1 = new System.Data.DataTable("Profile");
            ds.Tables.Add(table1);
            table1.Columns.Add("ID", typeof(string));
            table1.Columns.Add("a", typeof(string));
            System.Data.DataRow row1 = table1.NewRow();
            row1[0] = mds.fileName;
            row1[1] = Results["a"];
            ds.Tables["Profile"].Rows.Add(row1);

            System.Data.DataTable table2 = new System.Data.DataTable("Top");
            ds.Tables.Add(table2);
            table2.Columns.Add("Ao", typeof(string));
            table2.Columns.Add("ao", typeof(string));
            table2.Columns.Add("Iox", typeof(string));
            table2.Columns.Add("Ioy", typeof(string));
            table2.Columns.Add("Zoo", typeof(string));
            table2.Columns.Add("Zou", typeof(string));
            System.Data.DataRow row2 = table2.NewRow();
            row2[0] = Results["Ao"];
            row2[1] = Results["ao"];
            row2[2] = Results["Iox"];
            row2[3] = Results["Ioy"];
            row2[4] = Results["Zoo"];
            row2[5] = Results["Zou"];
            ds.Tables["Top"].Rows.Add(row2);

            System.Data.DataTable table3 = new System.Data.DataTable("Bottom");
            ds.Tables.Add(table3);
            table3.Columns.Add("Au", typeof(string));
            table3.Columns.Add("au", typeof(string));
            table3.Columns.Add("Iux", typeof(string));
            table3.Columns.Add("Iuy", typeof(string));
            table3.Columns.Add("Zuo", typeof(string));
            table3.Columns.Add("Zuu", typeof(string));
            System.Data.DataRow row3 = table3.NewRow();
            row3[0] = Results["Au"];
            row3[1] = Results["au"];
            row3[2] = Results["Iux"];
            row3[3] = Results["Iuy"];
            row3[4] = Results["Zuo"];
            row3[5] = Results["Zuu"];
            ds.Tables["Bottom"].Rows.Add(row3);

            string path = Environment.CurrentDirectory + optime;
            ds.WriteXml(path);
        }

        public void Export()
        {
            // Initialize the dataset
            this.mds = new MesherDataSet();

            string filename = mds.fileName;

            List<Polygon> polygons = dxfPolyNew(filename);


            double d = dxfPolydepth(filename);
            double Zoo = 0.0;
            double Zou = 0.0;
            double Zuo = 0.0;
            double Zuu = 0.0;

            writeDxf(filename);

            // Check if there are two profiles
            if (polygons.Count <= 2)
            {
                Console.WriteLine("THE FILE {0} DOES NOT CONTAIN TWO PROFILES", filename);
                Console.WriteLine();
            }

            // Iterate the top and bottom profile
            for (int i = 0; i <= 1; i++)
            {
                poly = polygons.ElementAt(i);
                mesh = DxfMesh(poly);

                // Compute mesh basic property
                MesherBasicProperty();

                // Write the results
                MesherWriteStat();

                double[] Z = singledepth(poly);

                if(i == 0)
                {
                    Zoo = Z.ElementAt(0);
                    Zou = Z.ElementAt(1);
                    Results.Add("Ao", mds.totalMeshArea / 100);
                    Results.Add("Iox", mds.meshIxxC / 10000);
                    Results.Add("Ioy", mds.meshIyyC / 10000);
                    Results.Add("CentroidXo", mds.meshCenteroidX);
                    Results.Add("CentroidYo", mds.meshCenteroidY);
                    Results.Add("Zoo", Zoo);
                    Results.Add("Zou", Zou);
                }

                if (i == 1)
                {
                    Zuo = Z.ElementAt(0);
                    Zuu = Z.ElementAt(1);
                    Results.Add("Au", mds.totalMeshArea / 100);
                    Results.Add("Iux", mds.meshIxxC / 10000);
                    Results.Add("Iuy", mds.meshIyyC / 10000);
                    Results.Add("CentroidXu", mds.meshCenteroidX);
                    Results.Add("CentroidYu", mds.meshCenteroidY);
                    Results.Add("Zuo", Zuo);
                    Results.Add("Zuu", Zuu);
                }
            }
            double a = d - Zoo - Zuu;
            Results.Add("a", a / 10);
            double ao = (Results["Au"] * a) / (Results["Au"] + Results["Ao"]);
            double au = a - ao;
            Results.Add("ao", ao / 10);
            Results.Add("au", au / 10);

        }
        
         
        #region Mesher functions

        // Open profile
        private static DxfDocument OpenProfile(string file)
        {
            // open the profile file
            FileInfo fileInfo = new FileInfo(file);

            // check if profile file is valid
            if (!fileInfo.Exists)
            {
                Console.WriteLine("THE FILE {0} DOES NOT EXIST", file);
                Console.WriteLine();
                return null;
            }

            DxfDocument dxf = DxfDocument.Load(file, new List<string> { @".\Support" });

            // check if there has been any problems loading the file,
            if (dxf == null)
            {
                Console.WriteLine("ERROR LOADING {0}", file);
                Console.WriteLine();
                Console.WriteLine("Press a key to continue...");
                Console.ReadLine();
                return null;
            }
            return dxf;
        }

        // Create new dxf file
        public void writeDxf(string filename)
        {

            DxfDocument dxf; 
            dxf = OpenProfile(filename);

            foreach (var ad in dims)
            {
                dxf.AddEntity(ad);
            }
            dxf.Save(filename);
        }


        // Store polygon from dxf file
        public List<Polygon> dxfPolyNew(string filename)
        {
            // read the dxf file
            DxfDocument dxfTest;
            dxfTest = OpenProfile(filename);

            int numberSegments = 128;
            int blockNumber = -1;

            Vertex polygonCentroid = new Vertex(0, 0);

            var polygons = new List<Polygon>();
            var Poly = new Polygon();

            var topVerticalLines = new List<HatchBoundaryPath.Line>();
            var topHorizontalLines = new List<HatchBoundaryPath.Line>();
            var bottomVerticalLines = new List<HatchBoundaryPath.Line>();
            var bottomHorizontalLines = new List<HatchBoundaryPath.Line>();
            //var topVerticalLinesID = new List<int>();
            //var topHorizontalLinesID = new List<int>();
            //var bottomVerticalLinesID = new List<int>();
            //var bottomHorizontalLinesID = new List<int>();

            // loop over all relevant blacks and store the hatch boundaries
            foreach (var bl in dxfTest.Blocks)
            {
                // loop over the enteties in the block and decompose them if they belong to an aluminum layer
                foreach (var ent in bl.Entities)
                {
                    if (ent.Layer.Name.ToString() == "0S-Alu hatch")
                    {
                        Poly = new Polygon();
                        blockNumber++;
                        HatchPattern hp = HatchPattern.Solid;
                        Hatch myHatch = new Hatch(hp, false);
                        myHatch = (Hatch)ent;
                        int pathNumber = -1;
                        

                        foreach (var bPath in myHatch.BoundaryPaths)
                        {
                            pathNumber++;
                            var contour = new List<Vertex>();
                            

                            // Store the contour
                            for (int i = 0; i < bPath.Edges.Count; i++)
                            {

                                switch (bPath.Edges[i].Type.ToString().ToLower())
                                {
                                    case "line":
                                        var myLine = (netDxf.Entities.HatchBoundaryPath.Line)bPath.Edges[i];
                                        var vLine = new Vertex();

                                        vLine.X = myLine.Start.X;
                                        vLine.Y = myLine.Start.Y;
                                        contour.Add(vLine);

                                        // Top profile
                                        if (blockNumber == 0)
                                        {
                                            // Vertical Line   
                                            if (myLine.Start.X == myLine.End.X)
                                            {
                                                topVerticalLines.Add(myLine);
                                            }
                                            // Horizontal Line
                                            if (myLine.Start.Y == myLine.End.Y)
                                            {
                                                topHorizontalLines.Add(myLine);
                                            }
                                        }
                                        // Bottom profile
                                        else
                                        {
                                            // Vertical Line   
                                            if (myLine.Start.X == myLine.End.X)
                                            {
                                                bottomVerticalLines.Add(myLine);
                                            }
                                            // Horizontal Line
                                            if (myLine.Start.Y == myLine.End.Y)
                                            {
                                                bottomHorizontalLines.Add(myLine);
                                            }
                                        }
                                        break;

                                    case "arc":
                                        var myArc = (netDxf.Entities.HatchBoundaryPath.Arc)bPath.Edges[i];
                                        double delta = (myArc.EndAngle - myArc.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vArc = new Vertex();
                                            double angleArc = (myArc.StartAngle + j * delta) * Math.PI / 180.0;
                                            if (myArc.IsCounterclockwise == true)
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(angleArc);
                                            }
                                            else
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(Math.PI + angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(Math.PI - angleArc);
                                            }
                                            contour.Add(vArc);
                                        }
                                        break;

                                    case "ellipse":
                                        var myEllipse = (netDxf.Entities.HatchBoundaryPath.Ellipse)bPath.Edges[i];
                                        double deltaEllipse = (myEllipse.EndAngle - myEllipse.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vEllipse = new Vertex();
                                            var ellipseRadius = Math.Sqrt(Math.Pow(myEllipse.EndMajorAxis.X, 2) + Math.Pow(myEllipse.EndMajorAxis.Y, 2));

                                            double angleEllipse = (myEllipse.StartAngle + j * deltaEllipse) * Math.PI / 180.0;
                                            if (myEllipse.IsCounterclockwise == true)
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(angleEllipse);
                                            }
                                            else
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(Math.PI + angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(Math.PI - angleEllipse);
                                            }
                                            contour.Add(vEllipse);
                                        }
                                        break;
                                }
                            }

                            bool hole = true;
                            // Add to the poly
                            if(blockNumber == 0 || blockNumber == 1)
                            {
                                if (pathNumber == 0)
                                {
                                    hole = false;
                                    polygonCentroid = MesherPolygonCentroid(contour);
                                }
                                for (int m = 0; m < contour.Count; m++)
                                {
                                    contour.ElementAt(m).X = contour.ElementAt(m).X - polygonCentroid.X;
                                    contour.ElementAt(m).Y = contour.ElementAt(m).Y - polygonCentroid.Y;
                                }
                                Poly.AddContour(points: contour, marker: 0, hole: hole);
                                centroids.Add(polygonCentroid);
                            }
                        }
                        polygons.Add(Poly);

                        int offset = 5;

                        double[] vector = dimDepth(Poly);

                        DimensionStyleOverrideDictionary overrides;
                        Vector2 vector1 = new Vector2();
                        Vector2 vector2 = new Vector2();

                        int rotation = 0;

                        if (blockNumber == 0)
                        {
                            rotation = 0;
                            vector1 = new Vector2(vector[0] + polygonCentroid.X, vector[4] + polygonCentroid.Y + 5);
                            vector2 = new Vector2(vector[2] + polygonCentroid.X, vector[4] + polygonCentroid.Y + 5);
                        }
                        else
                        {
                            rotation = 180;
                            vector1 = new Vector2(vector[0] + polygonCentroid.X, vector[5] + polygonCentroid.Y - 5);
                            vector2 = new Vector2(vector[2] + polygonCentroid.X, vector[5] + polygonCentroid.Y - 5);
                        }
                        LinearDimension ad = new LinearDimension(vector1, vector2, offset, rotation);

                        overrides = ad.StyleOverrides;
                        overrides.Add(DimensionStyleOverrideType.TextHeight, 2.5);
                        overrides.Add(DimensionStyleOverrideType.ArrowSize, 2.5);
                        overrides.Add(DimensionStyleOverrideType.DimRoundoff, 0.1);
                        overrides.Add(DimensionStyleOverrideType.SuppressLinearTrailingZeros, true);
                        overrides.Add(DimensionStyleOverrideType.TextOffset, 2.5);

                        var dimlayer = new Layer("DIM");
                        ad.Layer = dimlayer;

                        dims.Add(ad);
                    }
                }
            }
            return polygons;
        }


        // Sort the ID list to arrange the lines from left to right, top to bottom
        public int[] sort(List<HatchBoundaryPath.Line> lines, int[] id)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                id[i] = i;
            }

            int temp = 0;

            for(int i = 0; i < lines.Count; i++)
            {
                for (int j = 0; j < lines.Count; j++)
                {
                    if (lines.ElementAt(i).Start.X > lines.ElementAt(j).Start.X)
                    {
                        temp = id[i];
                        id[i] = id[j];
                        id[j] = temp;
                    }
                }
            }
            return id;
        }
         

        public double dxfPolydepth(string filename)
        {
            // read the dxf file
            DxfDocument dxfTest;
            var poly = new Polygon();
            dxfTest = OpenProfile(filename);
            int numberSegments = 16;
            int blockNumber = -1;

            // loop over all relevant blacks and store the hatch boundaries
            foreach (var bl in dxfTest.Blocks)
            {
                // loop over the enteties in the block and decompose them if they belong to an aluminum layer
                foreach (var ent in bl.Entities)
                {
                    if (ent.Layer.Name.ToString().Contains("0S-Alu hatch")) 
                    {
                        blockNumber++;

                        HatchPattern hp = HatchPattern.Solid;
                        Hatch myHatch = new Hatch(hp, false);
                        myHatch = (Hatch)ent;
                        int pathNumber = -1;

                        foreach (var bPath in myHatch.BoundaryPaths)
                        {
                            pathNumber++;
                            // define the contour list
                            var contour = new List<Vertex>();

                            for (int i = 0; i < bPath.Edges.Count; i++)
                            {
                                switch (bPath.Edges[i].Type.ToString().ToLower())
                                {
                                    case "line":
                                        var myLine = (netDxf.Entities.HatchBoundaryPath.Line)bPath.Edges[i];
                                        var vLine = new Vertex();
                                        vLine.X = myLine.Start.X;
                                        vLine.Y = myLine.Start.Y;
                                        contour.Add(vLine);
                                        break;

                                    case "arc":
                                        var myArc = (netDxf.Entities.HatchBoundaryPath.Arc)bPath.Edges[i];

                                        double delta = (myArc.EndAngle - myArc.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vArc = new Vertex();
                                            double angleArc = (myArc.StartAngle + j * delta) * Math.PI / 180.0;
                                            if (myArc.IsCounterclockwise == true)
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(angleArc);
                                            }
                                            else
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(Math.PI + angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(Math.PI - angleArc);
                                            }

                                            contour.Add(vArc);
                                        }
                                        break;

                                    case "ellipse":
                                        var myEllipse = (netDxf.Entities.HatchBoundaryPath.Ellipse)bPath.Edges[i];
                                        double deltaEllipse = (myEllipse.EndAngle - myEllipse.StartAngle) / numberSegments;


                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vEllipse = new Vertex();
                                            var ellipseRadius = Math.Sqrt(Math.Pow(myEllipse.EndMajorAxis.X, 2) + Math.Pow(myEllipse.EndMajorAxis.Y, 2));

                                            double angleEllipse = (myEllipse.StartAngle + j * deltaEllipse) * Math.PI / 180.0;
                                            if (myEllipse.IsCounterclockwise == true)
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(angleEllipse);
                                            }
                                            else
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(Math.PI + angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(Math.PI - angleEllipse);
                                            }

                                            contour.Add(vEllipse);
                                        }
                                        break;

                                }
                            }

                            bool hole = true;
                            
                            if (pathNumber == 0)
                            {
                                hole = false;
                            }
                            poly.AddContour(points: contour, marker: 0, hole: hole);
                        }
                    }
                }
            }

            // Get the depth
            double ytop = double.NegativeInfinity;
            double ybottom = double.PositiveInfinity;
            double xleft = double.PositiveInfinity;
            double xright = double.NegativeInfinity;
            double yleft = 0.0;
            foreach (var vertex in poly.Points)
            {
                if (xleft >= vertex.X)
                {
                    xleft = vertex.X;
                    yleft = vertex.Y;
                }
                if (xright <= vertex.X)
                {
                    xright = vertex.X;
                }
                if (ytop <= vertex.Y)
                {
                    ytop = vertex.Y;
                }
                if (ybottom >= vertex.Y)
                {
                    ybottom = vertex.Y;
                }
            }


            DimensionStyleOverrideDictionary overrides;
            LinearDimension ad = new LinearDimension(new Vector2(xleft - 5, ybottom), new Vector2(xleft - 5, ytop), 5, 90);
            overrides = ad.StyleOverrides;
            overrides.Add(DimensionStyleOverrideType.TextHeight, 2.5);
            overrides.Add(DimensionStyleOverrideType.ArrowSize, 2.5);
            overrides.Add(DimensionStyleOverrideType.DimRoundoff, 0.1);
            overrides.Add(DimensionStyleOverrideType.SuppressLinearTrailingZeros, true);

            overrides.Add(DimensionStyleOverrideType.TextOffset, 2.5);

            var dimlayer = new Layer("DIM");
            ad.Layer = dimlayer;
            dims.Add(ad);

            return ytop-ybottom;
        }


        public double[] singledepth(Polygon poly)
        {
            double ytop = double.NegativeInfinity;
            double ybottom = double.PositiveInfinity;
            foreach (var vertex in poly.Points)
            {
                if (ytop <= vertex.Y)
                {
                    ytop = vertex.Y;
                }
                if (ybottom >= vertex.Y)
                {
                    ybottom = vertex.Y;
                }
            }
            double[] results = { ytop - mds.meshCenteroidY, mds.meshCenteroidY - ybottom };
            return results;
        }


        public double[] dimDepth(Polygon poly)
        {

            double xleft = double.PositiveInfinity;
            double xright = double.NegativeInfinity;
            double yleft = 0.0;
            double yright = 0.0;
            double ytop = double.NegativeInfinity;
            double ybottom = double.PositiveInfinity;
            foreach (var vertex in poly.Points)
            {
                if (xleft >= vertex.X)
                {
                    xleft = vertex.X;
                    yleft = vertex.Y;
                }
                if (xright <= vertex.X)
                {
                    xright = vertex.X;
                }
                if (ytop <= vertex.Y)
                {
                    ytop = vertex.Y;
                }
                if (ybottom >= vertex.Y)
                {
                    ybottom = vertex.Y;
                }
            }

            yright = yleft;

            double[] results = {xleft, yleft, xright, yright, ytop, ybottom};
            return results;
        }


        public List<Line> lineDim(string filename)
        {
            // read the dxf file
            DxfDocument dxfTest;

            dxfTest = OpenProfile(filename);
            int numberSegments = 128;
            int blockNumber = -1;
            Vertex polygonCentroid = new Vertex(0, 0);
            var polygons = new List<Polygon>();
            var Poly = new Polygon();

            var lines = new List<Line>();

            // loop over all relevant blacks and store the hatch boundaries
            foreach (var bl in dxfTest.Blocks)
            {
                // loop over the enteties in the block and decompose them if they belong to an aluminum layer
                foreach (var ent in bl.Entities)
                {
                    if (ent.Layer.Name.ToString() == "0S-Alu hatch")
                    {
                        Poly = new Polygon();
                        blockNumber++;
                        HatchPattern hp = HatchPattern.Solid;
                        Hatch myHatch = new Hatch(hp, false);
                        myHatch = (Hatch)ent;
                        int pathNumber = -1;

                        foreach (var bPath in myHatch.BoundaryPaths)
                        {
                            pathNumber++;
                            var contour = new List<Vertex>();

                            // Store the contour
                            for (int i = 0; i < bPath.Edges.Count; i++)
                            {

                                switch (bPath.Edges[i].Type.ToString().ToLower())
                                {
                                    case "line":
                                        var myLine = (netDxf.Entities.HatchBoundaryPath.Line)bPath.Edges[i];
                                        var vLine = new Vertex();

                                        vLine.X = myLine.Start.X;
                                        vLine.Y = myLine.Start.Y;
                                        contour.Add(vLine);
                                        break;

                                    case "arc":
                                        var myArc = (netDxf.Entities.HatchBoundaryPath.Arc)bPath.Edges[i];
                                        double delta = (myArc.EndAngle - myArc.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vArc = new Vertex();
                                            double angleArc = (myArc.StartAngle + j * delta) * Math.PI / 180.0;
                                            if (myArc.IsCounterclockwise == true)
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(angleArc);
                                            }
                                            else
                                            {
                                                vArc.X = myArc.Center.X + myArc.Radius * Math.Cos(Math.PI + angleArc);
                                                vArc.Y = myArc.Center.Y + myArc.Radius * Math.Sin(Math.PI - angleArc);
                                            }
                                            contour.Add(vArc);
                                        }
                                        break;

                                    case "ellipse":
                                        var myEllipse = (netDxf.Entities.HatchBoundaryPath.Ellipse)bPath.Edges[i];
                                        double deltaEllipse = (myEllipse.EndAngle - myEllipse.StartAngle) / numberSegments;

                                        for (int j = 0; j < numberSegments; j++)
                                        {
                                            var vEllipse = new Vertex();
                                            var ellipseRadius = Math.Sqrt(Math.Pow(myEllipse.EndMajorAxis.X, 2) + Math.Pow(myEllipse.EndMajorAxis.Y, 2));

                                            double angleEllipse = (myEllipse.StartAngle + j * deltaEllipse) * Math.PI / 180.0;
                                            if (myEllipse.IsCounterclockwise == true)
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(angleEllipse);
                                            }
                                            else
                                            {
                                                vEllipse.X = myEllipse.Center.X + ellipseRadius * Math.Cos(Math.PI + angleEllipse);
                                                vEllipse.Y = myEllipse.Center.Y + ellipseRadius * Math.Sin(Math.PI - angleEllipse);
                                            }
                                            contour.Add(vEllipse);
                                        }
                                        break;
                                }
                            }

                            bool hole = true;
                            // Add to the poly
                            if(blockNumber == 0 || blockNumber == 1)
                            {
                                if (pathNumber == 0)
                                {
                                    hole = false;
                                    polygonCentroid = MesherPolygonCentroid(contour);
                                }
                                for (int m = 0; m < contour.Count; m++)
                                {
                                    contour.ElementAt(m).X = contour.ElementAt(m).X - polygonCentroid.X;
                                    contour.ElementAt(m).Y = contour.ElementAt(m).Y - polygonCentroid.Y;
                                }
                                Poly.AddContour(points: contour, marker: 0, hole: hole);
                                centroids.Add(polygonCentroid);
                            }
                        }
                        polygons.Add(Poly);
                    }
                }
            }
            return lines;
        }


        /// <summary>
        /// Given a filename, create a prefix for Three.js object
        /// </summary>
        /// <param name="filename"></param>
        /// <returns>a string in the format, e.g. art_466470</returns>
        public string getPrefix()
        {
            return "art_" + mds.fileName.Substring(0, 6);
        }


        /// <summary>
        /// Discover the dxf geometry information by blocks then by layers
        /// Find key layers with key material name characters, i.e. ALU, EPDM, PLASTIC
        /// Convert dxf entities to Three.js codes and add start moveto line
        /// Add key information for extrusion
        /// </summary>
        /// <param name="filename">the location of the target dxf file</param>
        public void dxfLayerSearch(string filename)
        {
            
            // read the dxf file
            DxfDocument dxfTest;
            dxfTest = OpenProfile(filename);

            if (dxfTest == null)
            {
                Console.WriteLine("dxf is null");
            }

            string prefix = getPrefix();
            contents.Add("function article__" + mds.fileName.Substring(0,6) + "() {");

            int AluminiumNum = -1;
            int IsolatiorNum = -1;
            int PEFoamNum = -1;

            foreach (var bl in dxfTest.Blocks)
            {
                foreach (var ent in bl.Entities)
                {
                    string layerPrefix = "";
                    // Console.WriteLine(ent.Layer.Name.ToString());
                    switch (ent.Layer.Name.ToString())
                    {
                        case "0S-Alu hatch":
                            AluminiumNum++;
                            layerPrefix = prefix + "_ProfileAluminium" + AluminiumNum;
                            break;

                        case "0S-Plastic hatch":
                            IsolatiorNum++;
                            layerPrefix = prefix + "_ProfileIsolator" + IsolatiorNum;
                            break;

                        case "0S-PT hatch":
                            PEFoamNum++;
                            layerPrefix = prefix + "_ProfilePEFoam" + PEFoamNum;
                            break;
                    }
                    /// Check if the layer is valid
                    if (layerPrefix == "")
                    {
                        //Console.WriteLine("NO LAYER DETECTED");
                        continue;
                    }

                    mds.shapes.Add(layerPrefix);
                    

                    EntitiesConvert(ent, layerPrefix);

                }
            }

            /// Add string: useMaterial
            for (int i = 0; i < AluminiumNum + 1; i++)
            {
                contents.Add("\t" + prefix + "_ProfileAluminium" + i + ".useMaterial = \"ProfileAluminium\";");
            }
            for (int i = 0; i < IsolatiorNum + 1; i++)
            {
                contents.Add("\t" + prefix + "_ProfileIsolator" + i + ".useMaterial = \"ProfileIsolator\";");
            }
            for (int i = 0; i < PEFoamNum + 1; i++)
            {
                contents.Add("\t" + prefix + "_ProfilePEFoam" + i + ".useMaterial = \"ProfilePEFoam\";");
            }

            contents.Add("\t" + "var " + prefix + " = {");
            contents.Add("\t\t" + "name: \"article__" + mds.articleNumber + "\",");
            contents.Add("\t\t" + "system: \"" + mds.system + "\",");
            //contents.Add("\t\t" + "type: \"" + mds.type + "\",");
            contents.Add("\t\t" + "width: \"" + 35 + "\",");
            contents.Add("\t\t" + "depth: \"" + mds.depth + "\",");
            //contents.Add("\t\t" + "insideWidth: \"" + 60 + "\",");
            //contents.Add("\t\t" + "outsideWidth: \"" + 60 + "\",");
            //contents.Add("\t\t" + "offsetReference: \"" + mds.offsetReference + "\",");
            //contents.Add("\t\t" + "anchorOffset: \"" + 50 + "\",");


            string shapes = "";
            foreach (var shape in mds.shapes)
            {
                shapes = shapes + shape + ", ";
            }
            contents.Add("\t\t" + "shapes: [" + shapes + "]");
            contents.Add("\t" + "};");
            contents.Add("\t" + "return " + prefix + ";");
            contents.Add("}");
        }


        /// <summary>
        /// Write Three.js geometry in each layer
        /// </summary>
        /// <param name="ent">dxf geometry information in one layer</param>
        /// <param name="layerPrefix">the prefix with artical number, layer name, layer index, e.g. art_184090_AL0</param>
        public void EntitiesConvert(EntityObject ent, string layerPrefix)
        {

            HatchPattern hp = HatchPattern.Solid;
            Hatch myHatch = new Hatch(hp, false);
            myHatch = (Hatch)ent;
            int pathNumber = -1;

            

            foreach (var bPath in myHatch.BoundaryPaths)
            {
                pathNumber++;
                List<string> insertContents = new List<string>();
                string insertPrefix = layerPrefix.Substring(0, 10) + "_ProfileInsert";
                

                if (pathNumber == 0)
                {
                    contents.Add("\t" + "var " + layerPrefix + " = new THREE.Shape();");
                }
                //else if(pathNumber == 1)
                //{
                //    contents.Add("\t" + "var " + layerPrefix + "Hole" + pathNumber + " = new THREE.Shape();");
                //    insertContents.Add("\t" + "var " + insertPrefix + " = new THREE.Shape();");
                //}
                else
                {
                    contents.Add("\t" + "var " + layerPrefix + "Hole" + pathNumber + " = new THREE.Shape();");
                }



                int startPoint = -1;
                // Store the contour
                for (int i = 0; i < bPath.Edges.Count; i++)
                {
                    startPoint++;
                    switch (bPath.Edges[i].Type.ToString().ToLower())
                    {
                        case "line":
                            var myLine = (HatchBoundaryPath.Line)bPath.Edges[i];

                            if (startPoint == 0)
                            {
                                if (pathNumber == 0)
                                {
                                    contents.Add("\t" + layerPrefix + ".moveTo(" + myLine.Start.X + ", " + myLine.Start.Y + ");");
                                }
                                else
                                {
                                    contents.Add("\t" + layerPrefix + "Hole" + pathNumber + ".moveTo(" + myLine.Start.X + ", " + myLine.Start.Y + ");");
                                    insertContents.Add("\t" + insertPrefix + ".moveTo(" + myLine.Start.X + ", " + myLine.Start.Y + ");");
                                }
                            }
                            if (pathNumber == 0)
                            {
                                contents.Add("\t" + layerPrefix + ".lineTo(" + myLine.End.X + ", " + myLine.End.Y + ");");
                            }
                            else
                            {
                                contents.Add("\t" + layerPrefix + "Hole" + pathNumber + ".lineTo(" + myLine.End.X + ", " + myLine.End.Y + ");");
                                insertContents.Add("\t" + insertPrefix + ".lineTo(" + myLine.End.X + ", " + myLine.End.Y + ");");
                            }
                            
                            break;

                        case "polyline":
                            var myPolyline = (HatchBoundaryPath.Polyline)bPath.Edges[i];

                            int pointCounter = 0;
                            foreach (var vertex in myPolyline.Vertexes) 
                            {
                                if (startPoint == 0 && pointCounter == 0)
                                {
                                    if (pathNumber == 0)
                                    {
                                        contents.Add("\t" + layerPrefix + ".moveTo(" + vertex.X * (-1) + ", " + vertex.Y + ");");
                                    }
                                    else
                                    {
                                        contents.Add("\t" + layerPrefix + "Hole" + pathNumber + ".moveTo(" + vertex.X * (-1) + ", " + vertex.Y + ");");
                                        insertContents.Add("\t" + insertPrefix + ".moveTo(" + vertex.X * (-1) + ", " + vertex.Y + ");");
                                    }
                                }
                                if (pathNumber == 0)
                                {
                                    contents.Add("\t" + layerPrefix + ".lineTo(" + vertex.X * (-1) + ", " + vertex.Y + ");");
                                }
                                else
                                {
                                    contents.Add("\t" + layerPrefix + "Hole" + pathNumber + ".lineTo(" + vertex.X * (-1) + ", " + vertex.Y + ");");
                                    insertContents.Add("\t" + insertPrefix + ".lineTo(" + vertex.X * (-1) + ", " + vertex.Y + ");");
                                }

                                pointCounter++;
                            }
                            break;


                        case "arc":
                            var myArc = (HatchBoundaryPath.Arc)bPath.Edges[i];

                            if (startPoint == 0)
                            {
                                if (pathNumber == 0)
                                {
                                    contents.Add("\t" + layerPrefix + ".moveTo(" + myArc.Center.X + ", " + myArc.Center.Y + ");");
                                }
                                else
                                {

                                    contents.Add("\t" + layerPrefix + "Hole" + pathNumber + ".moveTo(" + myArc.Center.X + ", " + myArc.Center.Y + ");");
                                    insertContents.Add("\t" + insertPrefix + ".moveTo(" + myArc.Center.X + ", " + myArc.Center.Y + ");");
                                }
                            }

                            if (pathNumber == 0)
                            {
                                contents.Add("\t" + layerPrefix + ".absarc(" + myArc.Center.X + ", " + myArc.Center.Y + ", " + myArc.Radius + ", " + myArc.StartAngle + ", " + myArc.EndAngle + ", " + myArc.IsCounterclockwise.ToString().ToLower() + ");");

                            }
                            else
                            {
                                contents.Add("\t" + layerPrefix + "Hole" + pathNumber + ".absarc(" + myArc.Center.X + ", " + myArc.Center.Y + ", " + myArc.Radius + ", " + myArc.StartAngle + ", " + myArc.EndAngle + ", " + myArc.IsCounterclockwise.ToString().ToLower() + ");");
                                insertContents.Add("\t" + insertPrefix + ".absarc(" + myArc.Center.X + ", " + myArc.Center.Y + ", " + myArc.Radius + ", " + myArc.StartAngle + ", " + myArc.EndAngle + ", " + myArc.IsCounterclockwise.ToString().ToLower() + ");");
                            }
                            break;


                        case "ellipse":
                            var myEllipse = (HatchBoundaryPath.Ellipse)bPath.Edges[i];

                            if (startPoint == 0)
                            {
                                if (pathNumber == 0)
                                {
                                    contents.Add("\t" + layerPrefix + ".moveTo(" + myEllipse.Center.X + ", " + myEllipse.Center.Y + ");");
                                }
                                else
                                {
                                    contents.Add("\t" + layerPrefix + "Hole" + pathNumber + ".moveTo(" + myEllipse.Center.X + ", " + myEllipse.Center.Y + ");");
                                    insertContents.Add("\t" + insertPrefix + ".moveTo(" + myEllipse.Center.X + ", " + myEllipse.Center.Y + ");");
                                }
                            }

                            if (pathNumber == 0)
                            {
                                contents.Add("\t" + layerPrefix + ".absellipse(" + myEllipse.Center.X + ", " + myEllipse.Center.Y + ", " + myEllipse.EndMajorAxis.X + ", " + myEllipse.EndMajorAxis.Y + ", " + myEllipse.StartAngle + ", " + myEllipse.EndAngle + ", " + myEllipse.IsCounterclockwise.ToString().ToLower() + ");");
                            }
                            else
                            {
                                contents.Add("\t" + layerPrefix + "Hole" + pathNumber + ".absellipse(" + myEllipse.Center.X + ", " + myEllipse.Center.Y + ", " + myEllipse.EndMajorAxis.X + ", " + myEllipse.EndMajorAxis.Y + ", " + myEllipse.StartAngle + ", " + myEllipse.EndAngle + ", " + myEllipse.IsCounterclockwise.ToString().ToLower() + ");");
                                insertContents.Add("\t" + insertPrefix + ".absellipse(" + myEllipse.Center.X + ", " + myEllipse.Center.Y + ", " + myEllipse.EndMajorAxis.X + ", " + myEllipse.EndMajorAxis.Y + ", " + myEllipse.StartAngle + ", " + myEllipse.EndAngle + ", " + myEllipse.IsCounterclockwise.ToString().ToLower() + ");");

                            }
                            break;
                    }
                }

                if (pathNumber != 0)
                {
                    contents.Add("\t" + layerPrefix + ".holes.push(" + layerPrefix + "Hole" + pathNumber + ");");
                }
                //if (pathNumber == 1)
                //{
                //    foreach (string insertContent in insertContents)
                //    {
                //        contents.Add(insertContent);
                //    }
                //    mds.shapes.Add(insertPrefix);
                //}

            }
        }

        // Generate mesh
        private TriangleNet.Mesh DxfMesh(Polygon poly)
        {
            // routine to generate a mesh from the contnet of poly
            // Set quality and constraint options.
            var options = new ConstraintOptions() { ConformingDelaunay = true };
            var quality = new QualityOptions() { MinimumAngle = 15.0, MaximumArea = mds.minimumMeshArea };

            // create the mesh
            mesh = (TriangleNet.Mesh)poly.Triangulate(options, quality);

            // make sure there are at least 1000 elements in the mesh
            while (mesh.Triangles.Count < 1000)
            {
                mds.minimumMeshArea = mds.minimumMeshArea / 2;
                quality.MaximumArea = mds.minimumMeshArea;
                mesh = (TriangleNet.Mesh)poly.Triangulate(options, quality);
            }

            // smooth the mesh
            var smoother = new SimpleSmoother();
            smoother.Smooth(mesh);

            return mesh;
        }

        

        private void MesherWriteStat()
        {
            int nt = mesh.Triangles.Count;


            string myFormat = "{0,10:n2}";

            depthLabel.Text = string.Format(myFormat, mds.meshDepth);
            widthLabel.Text = string.Format(myFormat, mds.meshWidth);
            areaLabel.Text = string.Format(myFormat, mds.totalMeshArea);

            centerXLabel.Text = string.Format(myFormat, (mds.meshCenteroidX));
            CenterYLabel.Text = string.Format(myFormat, (mds.meshCenteroidY));

            Ixx.Text = string.Format(myFormat, (mds.meshIxxC));
            Iyy.Text = string.Format(myFormat, (mds.meshIyyC));
            Ixy.Text = string.Format(myFormat, (mds.meshIxyC));

            rXLabel.Text = string.Format(myFormat, mds.meshRx);
            rYLabel.Text = string.Format(myFormat, mds.meshRy);

            sxpLabel.Text = string.Format(myFormat, mds.meshSpx);
            sxnLabel.Text = string.Format(myFormat, mds.meshSnx);
            sypLabel.Text = string.Format(myFormat, mds.meshSpy);
            synLabel.Text = string.Format(myFormat, mds.meshSny);

            if (MeshAnalysisFlag)
            {
                Jzz.Text = string.Format(myFormat,mds.meshJ);
                CwLabel.Text = string.Format(myFormat, mds.meshCw);
                XsLabel.Text = string.Format(myFormat, mds.meshXs);
                YsLabel.Text = string.Format(myFormat, mds.meshYs);
                BetaLabel.Text= string.Format(myFormat, mds.meshBeta);
            }
            else
            {
                Jzz.Text = "N/A";
                CwLabel.Text = "N/A";
                XsLabel.Text = "N/A";
                YsLabel.Text = "N/A";
            }


            label4.Text = "Band =           " + MesherBandwidth().ToString();
            label5.Text = "Nodes =        " + mesh.Vertices.Count.ToString();
            label8.Text = "Triangles =   " + mesh.Triangles.Count.ToString();
            label9.Text = "Edges =          " + mesh.Edges.Count().ToString();
            label10.Text= "Segments =   " + mesh.Segments.Count().ToString();

            eArea.Text = "Max Area =  " + mds.minimumMeshArea.ToString("E3");
        }

        private void MesherBasicProperty()
        {
            int nt = mesh.Triangles.Count;

            mds.totalMeshArea = 0;
            mds.meshCenteroidX = 0;
            mds.meshCenteroidY = 0;
            mds.meshIxx = 0;
            mds.meshIyy = 0;
            mds.meshIxy = 0;

            // set depth and width
            double xmin = 0;
            double xmax = 0;
            double ymin = 0;
            double ymax = 0;
            MeshBox(ref xmin, ref xmax, ref ymin, ref ymax);
            mds.meshDepth = Math.Abs(ymax - ymin);
            mds.meshWidth = Math.Abs(xmax - xmin);


            for (int i = 0; i < nt; i++)
            {
                MesherTriangle myTriangle = new MesherTriangle(mesh.Triangles.ElementAt(i));

                mds.totalMeshArea += myTriangle.Area;

                mds.meshCenteroidX += myTriangle.Xc * myTriangle.Area;
                mds.meshCenteroidY += myTriangle.Yc * myTriangle.Area;
                mds.meshIxx += myTriangle.Ixx();
                mds.meshIyy += myTriangle.Iyy();
                mds.meshIxy += myTriangle.Ixy();
            }


            mds.meshCenteroidX = (mds.meshCenteroidX / mds.totalMeshArea);
            mds.meshCenteroidY = (mds.meshCenteroidY / mds.totalMeshArea);

            mds.meshIxxC = mds.meshIxx - mds.totalMeshArea * mds.meshCenteroidY * mds.meshCenteroidY;
            mds.meshIyyC = mds.meshIyy - mds.totalMeshArea * mds.meshCenteroidX * mds.meshCenteroidX;
            mds.meshIxyC = mds.meshIxy - mds.totalMeshArea * mds.meshCenteroidX * mds.meshCenteroidY;

            mds.meshRx = Math.Sqrt((mds.meshIxxC / mds.totalMeshArea));
            mds.meshRy = Math.Sqrt((mds.meshIyyC / mds.totalMeshArea));

            mds.meshSpx = (mds.meshIxxC / (ymax - mds.meshCenteroidY));
            mds.meshSnx = (mds.meshIxxC / (mds.meshCenteroidY - ymin));
            mds.meshSpy = (mds.meshIyyC / (xmax - mds.meshCenteroidX));
            mds.meshSny = (mds.meshIyyC / (mds.meshCenteroidX - xmin));
        }
        
        #endregion
        
        #region Utility functions

        public void MeshBox(ref double minx, ref double maxx, ref double miny, ref double maxy)
        {
            // function to find the linits of mesh

            maxx = -1E30;
            maxy = -1E30;
            minx = +1E30;
            miny = +1E30;

            foreach (var t in mesh.Triangles)
            {
                for (int i = 0; i < 3; i++)
                {
                    minx = Math.Min(minx, t.GetVertex(i).X);
                    maxx = Math.Max(maxx, t.GetVertex(i).X);
                    miny = Math.Min(miny, t.GetVertex(i).Y);
                    maxy = Math.Max(maxy, t.GetVertex(i).Y);
                }
            }
        }

        public int MesherBandwidth()
        {
            int bw;
            bw = 0;
            foreach (var t in mesh.Triangles)
            {
                bw = Math.Max(bw, Math.Abs(t.GetVertex(0).ID - t.GetVertex(1).ID));
                bw = Math.Max(bw, Math.Abs(t.GetVertex(1).ID - t.GetVertex(2).ID));
                bw = Math.Max(bw, Math.Abs(t.GetVertex(2).ID - t.GetVertex(0).ID));
            }
            return bw;
        }

        public double[,] MesherStiff(TriangleNet.Topology.Triangle t)
        {
            double[,] s = new double[3, 3];
            double b0, b1, b2, c0, c1, c2;

            TriangleNet.Geometry.Point[] p = new TriangleNet.Geometry.Point[3];

            p[0] = t.GetVertex(0);
            p[1] = t.GetVertex(1);
            p[2] = t.GetVertex(2);

            b0 = p[1].Y - p[2].Y;
            b1 = p[2].Y - p[0].Y;
            b2 = p[0].Y - p[1].Y;

            c0 = -p[1].X + p[2].X;
            c1 = -p[2].X + p[0].X;
            c2 = -p[0].X + p[1].X;

            s[0, 0] = b0 * b0 + c0 * c0;
            s[0, 1] = b0 * b1 + c0 * c1;
            s[0, 2] = b0 * b2 + c0 * c2;

            s[1, 0] = s[0, 1];
            s[1, 1] = b1 * b1 + c1 * c1;
            s[1, 2] = b1 * b2 + c1 * c2;

            s[2, 0] = s[0, 2];
            s[2, 1] = s[1, 2];
            s[2, 2] = b2 * b2 + c2 * c2;

            // compute element  Area
            double area;
            area = ((p[0].X * (p[1].Y - p[2].Y)) +
                    (p[1].X * (p[2].Y - p[0].Y)) +
                    (p[2].X * (p[0].Y - p[1].Y))) / 2;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    s[i, j] = s[i, j] / (4 * area);
                }
            }
            return s;
        }

        public double MesherNuemann(int index0, int index1)
        {

            // Create Nuemann boundary condition

            double value;

            // get coordinates the boundary pair node
            double x0 = mesh.Vertices.ElementAt(index0).X;
            double y0 = mesh.Vertices.ElementAt(index0).Y;
            double x1 = mesh.Vertices.ElementAt(index1).X;
            double y1 = mesh.Vertices.ElementAt(index1).Y;

            // find the length
            double dx = x1 - x0;
            double dy = y1 - y0;
            double len = Math.Sqrt(Math.Pow((dx), 2) + Math.Pow((dy), 2));

            // find midpoint
            double xavg = (x1 + x0) / 2;
            double yavg = (y1 + y0) / 2;

            // get the Neumann surface constraint
            value = dy * yavg / len + dx * xavg / len;
            value = (value * len / 2);

            return value;
        }

        #endregion


        #region Graphics

        public void MesherCenterMark(Graphics g, System.Drawing.Point center, int size, Pen greenPen, Brush greenBrush)
        {
            g.DrawEllipse(greenPen, center.X - size / 2, center.Y - size / 2, size, size);
            g.DrawLine(greenPen, center.X - size / 2, center.Y, center.X + size / 2, center.Y);
            g.DrawLine(greenPen, center.X, center.Y - size / 2, center.X, center.Y + size / 2);

            g.FillPie(greenBrush, center.X - size / 2, center.Y - size / 2, size, size, 0, 90);
            g.FillPie(greenBrush, center.X - size / 2, center.Y - size / 2, size, size, 180, 90);
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

            Graphics g = e.Graphics;
            Pen greenPen = new Pen(Color.FromArgb(120, 184, 40), 1F);
            Pen orangePen = new Pen(Color.Orange, 1F);
            Pen grayPen = new Pen(Color.LightGray, 1F);
            Pen blackPen = new Pen(Color.Black, 2F);
            Pen greenPenThick = new Pen(Color.FromArgb(120, 184, 40), 2F);
            SolidBrush greenBrush = new SolidBrush(Color.FromArgb(120, 184, 40));
            Font drawFont = new Font("Arial", 10);
            StringFormat drawFormat = new StringFormat();
            SolidBrush orangeBrush = new SolidBrush(System.Drawing.Color.Orange);
            SolidBrush grayBrush = new SolidBrush(System.Drawing.Color.LightGray);



            // find the limits
            double minx = 0, maxx = 0, miny = 0, maxy = 0;
            MeshBox(ref minx, ref maxx, ref miny, ref maxy);

            maxx = Math.Max(Math.Abs(maxx), Math.Abs(minx));
            maxy = Math.Max(Math.Abs(maxy), Math.Abs(miny));
            // find the scale factor
            double scale;
            scale = .9 * Math.Min(panel1.Width / (2*maxx), panel1.Height / (2*maxy));

            // draw the triangles
            
            foreach (var t in mesh.Triangles)
            {
                if(PlotMesh)
                {
                    System.Drawing.Point p0 = MesherPoint(scale, t.GetVertex(0).X, t.GetVertex(0).Y, panel1.Width, panel1.Height);
                    System.Drawing.Point p1 = MesherPoint(scale, t.GetVertex(1).X, t.GetVertex(1).Y, panel1.Width, panel1.Height);
                    System.Drawing.Point p2 = MesherPoint(scale, t.GetVertex(2).X, t.GetVertex(2).Y, panel1.Width, panel1.Height);

                    g.DrawLine(greenPen, p0, p1);
                    g.DrawLine(greenPen, p1, p2);
                    g.DrawLine(greenPen, p2, p0);
                }
            }


            // write the nodes
            if (PlotNodeNumber)
            {
                int i = 0;
                foreach (var vertix in mesh.Vertices)
                {
                    string nodeLabel = i.ToString() + "/ ID:" + vertix.ID.ToString();
                    System.Drawing.Point p = MesherPoint(scale, vertix.X, vertix.Y, panel1.Width, panel1.Height);
                    g.DrawString(nodeLabel, drawFont, grayBrush, p.X, p.Y, drawFormat);
                    i++;
                }
            }

            // reindex the nodes in the event renumbering is done
            int[] nodeIndex = new int[mesh.Vertices.Count];
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                nodeIndex[mesh.Vertices.ElementAt(i).ID] = i;
            }
            
            
            foreach (var edge in mesh.Edges)
            {

                int index0 = nodeIndex[edge.P0];
                int index1 = nodeIndex[edge.P1];
                int elabel = edge.Label;

                if (elabel == 1)
                {
                    System.Drawing.Point p0 = MesherPoint(scale, mesh.Vertices.ElementAt(index0).X,
                                                           mesh.Vertices.ElementAt(index0).Y,
                                                           panel1.Width, panel1.Height);
                    System.Drawing.Point p1 = MesherPoint(scale, mesh.Vertices.ElementAt(index1).X,
                                                                mesh.Vertices.ElementAt(index1).Y,
                                                                panel1.Width, panel1.Height);
                    g.DrawLine(greenPenThick, p0, p1);

                }
            }
            
            // render the mesh  function 
            if (PlotWarpingFunction)
            {
                MesherRenderMesh(g, scale,meshOmega,"Warping");
            }
            else if (PlotNormalStress)
            {
                MesherRenderMesh(g, scale, meshNormalStress,"NormalStress");
            }


            // draw the center mark
            System.Drawing.Point center = MesherPoint(scale, mds.meshCenteroidX-mds.meshCenteroidX, mds.meshCenteroidY-mds.meshCenteroidY, panel1.Width, panel1.Height);
            MesherCenterMark(g, center, 20, grayPen, grayBrush);
            System.Drawing.Point shearCenter = MesherPoint(scale, mds.meshXs, mds.meshYs, panel1.Width, panel1.Height);
            MesherCenterMark(g, shearCenter, 20, orangePen, orangeBrush);
        }

        private void MesherRenderMesh(Graphics g, double scale, double[] plotFunction,string plotType)
        {
            // routine to creat a thermograph of the waping or stress function

            // find the maximum and minumum of the warping function
            double maxPlot= -1.0E30;
            double minPlot = +1.0E30;
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                maxPlot = Math.Max(maxPlot, plotFunction[i]);
                minPlot = Math.Min(minPlot, plotFunction[i]);
            }
            //maxPlot = Math.Max(Math.Abs(maxPlot), Math.Abs(minPlot));
            //minPlot = -maxPlot;
            if (maxPlot == minPlot) { minPlot = -maxPlot; }
            if (Math.Abs(maxPlot-minPlot) < 0.0001)
            {
                minPlot = -1;
                maxPlot = +1;
            }
            foreach (var t in mesh.Triangles)
            {
                System.Drawing.Point[] points =
                    {
                           MesherPoint(scale, t.GetVertex(0).X, t.GetVertex(0).Y, panel1.Width, panel1.Height),
                           MesherPoint(scale, t.GetVertex(1).X, t.GetVertex(1).Y, panel1.Width, panel1.Height),
                           MesherPoint(scale, t.GetVertex(2).X, t.GetVertex(2).Y, panel1.Width, panel1.Height)
                        };


                if (MesherCollinear(points[0].X, points[0].Y, points[1].X, points[1].Y, points[2].X, points[2].Y))

                {
                    Double functionCenter = (plotFunction[t.GetVertexID(0)] + plotFunction[t.GetVertexID(1)] + plotFunction[t.GetVertexID(2)]) / 3;
                    double[] PV = { plotFunction[t.GetVertexID(0)], plotFunction[t.GetVertexID(1)], plotFunction[t.GetVertexID(2)] };
                    double PC = functionCenter;

                    Color SC = Color.Green;
                    Color EC = Color.Red;
                    Color MC = Color.Yellow;

                    if (plotType == "Warping")
                    {
                       SC = Color.Crimson;
                       EC = Color.RoyalBlue;
                       MC = Color.Yellow;
                    }
                   

                    Color[] colors = new Color[3];
                    colors[0] = MesherGetColor(PV[0], maxPlot, minPlot, SC, MC, EC);
                    colors[1] = MesherGetColor(PV[1], maxPlot, minPlot, SC, MC, EC);
                    colors[2] = MesherGetColor(PV[2], maxPlot, minPlot, SC, MC, EC);
                    Color CC = MesherGetColor(PC, maxPlot, minPlot, SC, MC, EC);

                    GraphicsPath gp = new GraphicsPath();

                    gp.AddLines(points);

                    PathGradientBrush gpb = new PathGradientBrush(gp);
                    gpb.CenterColor = CC;
                    gpb.SurroundColors = colors;
                    g.FillPath(gpb, gp);
                }

            }
        }

        private Color MesherGetColor(double value, double valueMax, double valueMin, Color startColor, Color centerColor, Color endColor)
        {


            int R, G, B;
            double factor = (value - valueMin) / (valueMax - valueMin);
            R = startColor.R + Convert.ToInt32((endColor.R - startColor.R) * factor);
            G = startColor.G + Convert.ToInt32((endColor.G - startColor.G) * factor);
            B = startColor.B + Convert.ToInt32((endColor.B - startColor.B) * factor);

            Color aColor = Color.FromArgb(R, G, B);

            return aColor;
        }

        private System.Drawing.Point MesherPoint(double scale, double x, double y, int width, int height)
        {
            System.Drawing.Point pnt = new System.Drawing.Point();
            pnt.X = width / 2 + Convert.ToInt32(x * scale);
            pnt.Y = height / 2 - Convert.ToInt32(y * scale);
            return pnt;
        }

        public bool MesherCollinear(int x1, int y1, int x2, int y2, int x3, int y3)
        {
            bool value;
            int a = x1 * (y2 - y3) +
                    x2 * (y3 - y1) +
                    x3 * (y1 - y2);

            if (a == 0)
            {
                value = false;
            }
             else
            {
                value = true;
            }
            return value;
        }

        public void MesherPlotStress()
        {
            // initialize the Normal Stress Vector
            int nv = mesh.Vertices.Count;
            Array.Resize(ref meshNormalStress, nv);
            for (int i = 0; i < nv; i++)
            {
                meshNormalStress[i] = 0.0;
            }


            // compute normal stresses
            appliedForceLocationX = Convert.ToDouble(appLocationX.Text);
            appliedForceLocationY = Convert.ToDouble(appLocationY.Text);
            appliedAxialForce = Convert.ToDouble(appPz.Text);
            appliedMomentX = Convert.ToDouble(appMx.Text);
            appliedMomentY = Convert.ToDouble(appMy.Text);

            double PZ = appliedAxialForce;
            double MX = appliedAxialForce * (appliedForceLocationY - mds.meshCenteroidY) + appliedMomentX;
            double MY = appliedAxialForce * (appliedForceLocationX - mds.meshCenteroidX) + appliedMomentY;

            double IXX = mds.meshIxx;
            double IYY = mds.meshIxx;
            double IXY = mds.meshIxy;
            double MA = mds.totalMeshArea;

            for (int i = 0; i < nv; i++)
            {
                int index = mesh.Vertices.ElementAt(i).ID;
                double xCoor = mesh.Vertices.ElementAt(i).X;
                double yCoor = mesh.Vertices.ElementAt(i).Y;
                double DX = xCoor - mds.meshCenteroidX;
                double DY = yCoor - mds.meshCenteroidY;

                meshNormalStress[index] = PZ / MA + ((MY * IXX + MX * IXY) * DX - (MX * IYY + MY * IXY) * DY) / (IXX * IYY - IXY * IXY);
            }
        }
        #endregion


        #region Navigation
        private void feild_KeyPress(object sender, KeyPressEventArgs e)
        {
            char ch = e.KeyChar;
            if (!Char.IsDigit(ch) && ch != 8 && ch != 46 && ch != 110 && ch != 85)
            {
                e.Handled = true;
            }
        }

        private void MeshStatLabel_Click(object sender, EventArgs e)
        {
            panel3.Visible = true;
        }

        private void ExitLabel_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void WarpingLabel_Click(object sender, EventArgs e)
        {
            if (PlotWarpingFunction)
            {
                PlotWarpingFunction = false;
                PlotNormalStress = false;
                PlotMesh = false;
            }
            else
            {
                PlotWarpingFunction = true;
                PlotNormalStress = false;
                PlotMesh = false;
            }


            panel1.Refresh();
        }

        private void FineMwsh_Click(object sender, EventArgs e)
        {
            PlotWarpingFunction = false;
            PlotNormalStress = false;
            PlotMesh = true;
            mds.minimumMeshArea = mds.minimumMeshArea / 2;
            var quality = new QualityOptions() { MinimumAngle = 15.0, MaximumArea = mds.minimumMeshArea };
            mesh.Refine(quality, true);
            var smoother = new SimpleSmoother();
            smoother.Smooth(mesh);
            panel1.Refresh();
            MesherBasicProperty();
            MeshAnalysisFlag = true;
            MesherWriteStat();
        }

        private void CoarseMesh_Click(object sender, EventArgs e)
        {
            PlotWarpingFunction = false;
            PlotNormalStress = false;
            PlotMesh = true;
            mds.minimumMeshArea = 0.5;
            var mesh = DxfMesh(poly);
            // write mesh stat
            panel1.Refresh();
            MesherBasicProperty();
            //     MeshAnalysisFlag = false;
            MesherWriteStat();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            panel3.Visible = false;
            appLocationX.Text = mds.meshCenteroidX.ToString("F4");
            appLocationY.Text = mds.meshCenteroidY.ToString("F4");

            // set the plot flag
            if (PlotNormalStress)
            {
                PlotNormalStress = false;
                PlotWarpingFunction = false;
                PlotMesh = false;
                AppliedLoadPanel.Visible = false;
            }
            else
            {
                PlotNormalStress = true;
                PlotWarpingFunction = false;
                PlotMesh = false;
                AppliedLoadPanel.Visible = true;
            }

            // compute stresses
            MesherPlotStress();

            // repaint the panel1
            panel1.Refresh();

        }

        private void textBox_validating(object sender, CancelEventArgs e)
        {
            double newDouble, upperLimit, lowerLimit;
            TextBox currenttb = (TextBox)sender;
            List<double> limits = (currenttb.Tag.ToString()).Split(',').Select(double.Parse).ToList();

            lowerLimit = limits[0];
            upperLimit = limits[1];

            if (!double.TryParse(currenttb.Text, out newDouble) || newDouble < lowerLimit || newDouble > upperLimit || string.IsNullOrEmpty(currenttb.Text))
            {
                e.Cancel = true;
                MessageBox.Show("value must be between " + lowerLimit.ToString("G") + " and " + upperLimit.ToString("G"));
            }
            else
            {
            }
        }

        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.SelectNextControl((Control)sender, true, true, true, true);
                e.SuppressKeyPress = true;
            }
        }

        private void Cell_Enter(object sender, EventArgs e)
        {
            TextBox currenttb = (TextBox)sender;
            currentCellValue = Convert.ToDouble(currenttb.Text);
        }

        private void Cell_Leave(object sender, EventArgs e)
        {
            TextBox currenttb = (TextBox)sender;
            double newCellValue = Convert.ToDouble(currenttb.Text);
            if(newCellValue!= currentCellValue)
            {
                MesherPlotStress();
                panel1.Refresh();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            panel3.Visible = false;
        }

        #endregion

        public Vertex MesherPolygonCentroid(List<Vertex> contour)
        {
            Vertex VC = new Vertex(0, 0);
            int np = contour.Count;
            double area = 0;
            for (int i = 0; i < np-1 ; i++)
            {
                area += contour.ElementAt(i).X * contour.ElementAt(i + 1).Y - contour.ElementAt(i).Y * contour.ElementAt(i + 1).X;
            }
            area += contour.ElementAt(np-1).X * contour.ElementAt(0).Y - contour.ElementAt(np-1).Y * contour.ElementAt(0).X;
            area = area / 2;

            for (int i = 0; i < np-1 ; i++)
            {
                VC.X += (contour.ElementAt(i).X + contour.ElementAt(i + 1).X)
                      * (contour.ElementAt(i).X * contour.ElementAt(i + 1).Y - contour.ElementAt(i).Y * contour.ElementAt(i + 1).X);
                VC.Y += (contour.ElementAt(i).Y + contour.ElementAt(i + 1).Y)
                      * (contour.ElementAt(i).X * contour.ElementAt(i + 1).Y - contour.ElementAt(i).Y * contour.ElementAt(i + 1).X);
            }

            VC.X += (contour.ElementAt(np - 1).X + contour.ElementAt(0).X)
                  * (contour.ElementAt(np - 1).X * contour.ElementAt(0).Y - contour.ElementAt(np - 1).Y * contour.ElementAt(0).X);
            VC.Y += (contour.ElementAt(np - 1).Y + contour.ElementAt(0).Y)
                  * (contour.ElementAt(np - 1).X * contour.ElementAt(0).Y - contour.ElementAt(np - 1).Y * contour.ElementAt(0).X);

            VC.X = VC.X / (6 * area);
            VC.Y = VC.Y / (6 * area);

            return VC;
        }
    }
}

