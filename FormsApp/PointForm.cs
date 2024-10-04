using Newtonsoft.Json;
using PointLib;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Formatters.Soap;
using System.Windows.Forms;
using System.Xml.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Collections.Generic;

namespace FormsApp
{
    public partial class PointForm : Form
    {
        private Point[] points = null;
        public PointForm()
        {
            InitializeComponent();
        }

        private void btnCreate_Click(object sender, System.EventArgs e)
        {
            points = new Point[5];

            var rnd = new Random();

            for (int i = 0; i < points.Length; i++)
                points[i] = rnd.Next(3) % 2 == 0 ? new Point() : new Point3D();

            listBox.DataSource = points;
        }

        private void btnSort_Click(object sender, EventArgs e)
        {
            if (points == null)
                return;

            Array.Sort(points);

            listBox.DataSource = null;
            listBox.DataSource = points;

        }

        private void btnSerialize_Click(object sender, EventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Filter = "SOAP|*.soap|XML|*.xml|JSON|*.json|Binary|*.bin|YAML|*.yaml|CustomFormat|*.myml"
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            using (var fs = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write))
            {
                switch (Path.GetExtension(dlg.FileName))
                {
                    case ".bin":
                        var bf = new BinaryFormatter();
                        bf.Serialize(fs, points);
                        break;
                    case ".soap":
                        var sf = new SoapFormatter();
                        sf.Serialize(fs, points);
                        break;
                    case ".xml":
                        var xf = new XmlSerializer(typeof(Point[]), new[] { typeof(Point3D) });
                        xf.Serialize(fs, points);
                        break;
                    case ".json":
                        var jf = new JsonSerializer { TypeNameHandling = TypeNameHandling.All };
                        using (var w = new StreamWriter(fs))
                        using (var jsonWriter = new JsonTextWriter(w))
                        {
                            jf.Serialize(jsonWriter, points);
                        }
                        break;
                    case ".yaml":
                        var serializer = new SerializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .WithTagMapping("!Point3D", typeof(Point3D))
                            .WithTagMapping("!Point", typeof(Point))
                            .Build();
                        using (var writer = new StreamWriter(fs))
                        {
                            serializer.Serialize(writer, points);
                        }
                        break;
                    case ".myml":
                        SerializeToCustomFormat(fs);
                        break;
                }
            }
        }

        private void btnDeserialize_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "SOAP|*.soap|XML|*.xml|JSON|*.json|Binary|*.bin|YAML|*.yaml|CustomFormat|*.myml"
            };

            if (dlg.ShowDialog() != DialogResult.OK)
                return;

            using (var fs = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read))
            {
                switch (Path.GetExtension(dlg.FileName))
                {
                    case ".bin":
                        var bf = new BinaryFormatter();
                        points = (Point[])bf.Deserialize(fs);
                        break;
                    case ".soap":
                        var sf = new SoapFormatter();
                        points = (Point[])sf.Deserialize(fs);
                        break;
                    case ".xml":
                        var xf = new XmlSerializer(typeof(Point[]), new[] { typeof(Point3D) });
                        points = (Point[])xf.Deserialize(fs);
                        break;
                    case ".json":
                        var jf = new JsonSerializer { TypeNameHandling = TypeNameHandling.All };
                        using (var r = new StreamReader(fs))
                        using (var jsonReader = new JsonTextReader(r))
                        {
                            points = (Point[])jf.Deserialize(jsonReader, typeof(Point[]));
                        }
                        break;
                    case ".yaml":
                        var deserializer = new DeserializerBuilder()
                            .WithNamingConvention(CamelCaseNamingConvention.Instance)
                            .WithTagMapping("!Point3D", typeof(Point3D))
                            .WithTagMapping("!Point", typeof(Point))
                            .Build();
                        using (var reader = new StreamReader(fs))
                        {
                            points = deserializer.Deserialize<Point[]>(reader);
                        }
                        break;
                    case ".myml":
                        DeserializeFromCustomFormat(fs);
                        break;
                }
            }

            listBox.DataSource = null;
            listBox.DataSource = points;
        }

        private void SerializeToCustomFormat(FileStream fs)
        {
            using (var writer = new StreamWriter(fs))
            {
                foreach (var point in points)
                {
                    if (point is Point3D p3d)
                    {
                        writer.WriteLine($"(Point3D-{p3d.X}:{p3d.Y}:{p3d.Z})");
                    }
                    else
                    {
                        writer.WriteLine($"(Point-{point.X}:{point.Y})");
                    }
                }
            }
        }

        private void DeserializeFromCustomFormat(FileStream fs)
        {
            using (var reader = new StreamReader(fs))
            {
                var list = new List<Point>();
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim('(', ')');
                    var parts = line.Split('-');

                    if (parts[0] == "Point3D")
                    {
                        var coords = parts[1].Split(':');
                        var x = int.Parse(coords[0]);
                        var y = int.Parse(coords[1]);
                        var z = int.Parse(coords[2]);
                        list.Add(new Point3D(x, y, z));
                    }
                    else if (parts[0] == "Point")
                    {
                        var coords = parts[1].Split(':');
                        var x = int.Parse(coords[0]);
                        var y = int.Parse(coords[1]);
                        list.Add(new Point(x, y));
                    }
                }

                points = list.ToArray();
            }
        }
    }
}
