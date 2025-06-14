using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace FaceDetectionApp.Models
{
    public class DetectedNose
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
    }

    public class FaceDetectionModel
    {
        public string InputPath { get; set; }
        public string ResultPath { get; set; }
        public List<DetectedNose> Noses { get; set; } = new();
        public bool IsVideo { get; set; }
    }
}

