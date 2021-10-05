using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Mesher
{
    class MesherTriangle
    {
        TriangleNet.Geometry.Point P0 = new TriangleNet.Geometry.Point();
        TriangleNet.Geometry.Point P1 = new TriangleNet.Geometry.Point();
        TriangleNet.Geometry.Point P2 = new TriangleNet.Geometry.Point();
       
        public Double Area { get; set; }
        public Double Xc { get; }
        public Double Yc { get; }
       

        public MesherTriangle(TriangleNet.Topology.Triangle triangle)
        {
            P0 = triangle.GetVertex(0);
            P1 = triangle.GetVertex(1);
            P2 = triangle.GetVertex(2);

            Area = ((P0.X * (P1.Y - P2.Y)) + (P1.X * (P2.Y - P0.Y)) + (P2.X * (P0.Y - P1.Y))) / 2;

            Xc = (P0.X + P1.X + P2.X) / 3;
            Yc = (P0.Y + P1.Y + P2.Y) / 3;
        }


        public double Ixx()
        {
            double result =0;
            result =(Area/6)* (P0.Y * P0.Y + P1.Y * P1.Y + P2.Y * P2.Y +
                               P0.Y * P1.Y + P1.Y * P2.Y + P2.Y * P0.Y);
            return result;

        }

        public double Iyy()
        {
            double result = 0;
            result = (Area / 6) * (P0.X * P0.X + P1.X * P1.X + P2.X * P2.X +
                                   P0.X * P1.X + P1.X * P2.X + P2.X * P0.X );
            return result;
        }
        public double Ixy()
        {
            double result = 0;
            result = (Area / 6) * (P0.X * P0.Y + P1.X * P1.Y + P2.X * P2.Y) +
                     (Area /12) * (P0.X * P1.Y + P1.X * P0.Y +
                                   P0.X * P2.Y + P2.X * P0.Y +
                                   P1.X * P2.Y + P2.X * P1.Y );
            return result;
        }
       
    }
    
}
