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

namespace Mesher
{
    public class MesherDataSet
    {
        public MesherDataSet()
        {
            fileName = "477770.dxf";
            articleNumber = fileName.Substring(0, 6);
            shapes = new List<string>();
            system = "FWS 60";

            depth = 0;
            insideWidth = 0;
            outsideWidth = 0;


            meshDepth = 0;//whole
            meshWidth = 0;
            totalMeshArea = 0;//
            meshCenteroidX = 0;//
            meshCenteroidY = 0;//

            meshIxx = 0;
            meshIyy = 0;
            meshIxy = 0;
            meshIxxC = 0;//
            meshIyyC = 0;//
            meshIxyC = 0;
            meshRx = 0;
            meshRy = 0;

            meshSpx = 0;
            meshSnx = 0;
            meshSpy = 0;
            meshSny = 0;


            meshJ = 0;
            meshCw = 0;
            meshXs = 0;
            meshYs = 0;
            meshBeta = 0;

            minimumMeshArea = 50.0;

        }

        public MesherDataSet(string _fileName)
        {
            fileName = _fileName;
            articleNumber = fileName.Substring(0, 6);
            shapes = new List<string>();
            system = "UDC 80";

            depth = 0;
            insideWidth = 0;
            outsideWidth = 0;


            meshDepth = 0;//whole
            meshWidth = 0;
            totalMeshArea = 0;//
            meshCenteroidX = 0;//
            meshCenteroidY = 0;//

            meshIxx = 0;
            meshIyy = 0;
            meshIxy = 0;
            meshIxxC = 0;//
            meshIyyC = 0;//
            meshIxyC = 0;
            meshRx = 0;
            meshRy = 0;

            meshSpx = 0;
            meshSnx = 0;
            meshSpy = 0;
            meshSny = 0;


            meshJ = 0;
            meshCw = 0;
            meshXs = 0;
            meshYs = 0;
            meshBeta = 0;

            minimumMeshArea = 50.0;

        }

        // mouse location

        public Point mouseLocation { get; set; }
        public Point formLocation { get; set; }

        // information data

        public string projectTitle { get; set; }
        public string sectionID { get; set; }

        public string fileName { get; set; }

        public double meshDepth { get; set; }
        public double meshWidth { get; set; }
        public double totalMeshArea { get; set; }
        public double meshCenteroidX { get; set; }
        public double meshCenteroidY { get; set; }

        public double meshIxx { get; set; }
        public double meshIyy { get; set; }
        public double meshIxy { get; set; }
        public double meshIxxC { get; set; }
        public double meshIyyC { get; set; }
        public double meshIxyC { get; set; }
        public double meshRx { get; set; }
        public double meshRy { get; set; }

        public double meshSpx { get; set; }
        public double meshSnx { get; set; }
        public double meshSpy { get; set; }
        public double meshSny { get; set; }


        public double meshJ { get; set; }
        public double meshCw { get; set; }
        public double meshXs { get; set; }
        public double meshYs { get; set; }
        public double meshBeta { get; set; }

        public double minimumMeshArea { get; set; }


        // Three Js converter data
        public string articleNumber { get; set; }
        public string system { get; set; }
        public string type { get; set; }


        public double depth { get; set; }
        public double insideWidth { get; set; }
        public double outsideWidth { get; set; }

        public string offsetReference { get; set; }

        public List<string> shapes { get; set; }


    }
}
