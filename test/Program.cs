using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HL7.Dotnetcore.Test
{
    [TestClass]
    public class HL7Test
    {
        private string HL7_ORM;
        private string HL7_ADT;

        public static void Main(string[] args)
        {
            var test = new HL7Test();
        }

        public HL7Test()
        {
            var path = Path.GetDirectoryName(typeof(HL7Test).GetTypeInfo().Assembly.Location) + "/";
            var message = ParseHL7(path + "example.hl7");
        }

        [TestMethod]
        public void SmokeTest()
        {
            Message message = new Message(this.HL7_ORM);
            Assert.IsNotNull(message);

            // message.ParseMessage();
            // File.WriteAllText("SmokeTestResult.txt", message.SerializeMessage(false));
        }

        [TestMethod]
        public void ParseTest1()
        {
            var message = new Message(this.HL7_ORM);

            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
        }

        [TestMethod]
        public void ParseTest2()
        {
            var message = new Message(this.HL7_ADT);

            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
        }


        [TestMethod]
        public void ReadSegmentTest()
        {
            var message = new Message(this.HL7_ORM);
            message.ParseMessage();

            Segment MSH_1 = message.Segments("MSH")[0];
            Assert.IsNotNull(MSH_1);
        }

        [TestMethod]
        public void ReadDefaultSegmentTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            Segment MSH = message.DefaultSegment("MSH");
            Assert.IsNotNull(MSH);
        }

        [TestMethod]
        public void ReadFieldTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_9 = message.GetValue("MSH.9");
            Assert.AreEqual("ADT^O01", MSH_9);
        }

        [TestMethod]
        public void ReadComponentTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_9_1 = message.GetValue("MSH.9.1");
            Assert.AreEqual("ADT", MSH_9_1);
        }

        [TestMethod]
        public void AddComponentsTest()
        {
            var encoding = new HL7Encoding();
            
            // Create a Segment with name ZIB
            Segment newSeg = new Segment("ZIB", encoding);

            // Create Field ZIB_1
            Field ZIB_1 = new Field("ZIB1", encoding);
            // Create Field ZIB_5
            Field ZIB_5 = new Field("ZIB5", encoding);

            // Create Component ZIB.5.3
            Component com1 = new Component("ZIB.5.3_", encoding);

            // Add Component ZIB.5.3 to Field ZIB_5
            ZIB_5.AddNewComponent(com1, 3);

            // Overwrite the same field again
            ZIB_5.AddNewComponent(new Component("ZIB.5.3", encoding), 3);

            // Add Field ZIB_1 to segment ZIB, this will add a new filed to next field location, in this case first field
            newSeg.AddNewField(ZIB_1);

            // Add Field ZIB_5 to segment ZIB, this will add a new filed as 5th field of segment
            newSeg.AddNewField(ZIB_5, 5);

            // Add segment ZIB to message
            var message = new Message(this.HL7_ADT);
            message.AddNewSegment(newSeg);

            string serializedMessage = message.SerializeMessage(false);
            Assert.AreEqual("ZIB|ZIB1||||ZIB5^^ZIB.5.3\r", serializedMessage);
        }

        [TestMethod]
        public void EmptyFieldsTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var NK1 = message.DefaultSegment("NK1").GetAllFields();
            Assert.AreEqual(34, NK1.Count);
            Assert.AreEqual(string.Empty, NK1[33].Value);
        }

        [TestMethod]
        public void EncodingForOutputTest()
        {
            const string oruUrl = "domain.com/resource.html?Action=1&ID=2";  // Text with special character (&)
            
            var obx = new Segment("OBX", new HL7Encoding());
            obx.AddNewField("1");
            obx.AddNewField("RP");
            obx.AddNewField("70030^Radiologic Exam, Eye, Detection, FB^CDIRadCodes");
            obx.AddNewField("1");
            obx.AddNewField(obx.Encoding.Encode(oruUrl));  // Encoded field
            obx.AddNewField("F", 11);
            obx.AddNewField(MessageHelper.LongDateWithFractionOfSecond(DateTime.Now), 14);            

            var oru = new Message();
            oru.AddNewSegment(obx);

            var str = oru.SerializeMessage(false);

            Assert.IsFalse(str.Contains("&"));  // Should have \T\ instead
        }
        
        [TestMethod]
        public void AddFieldTest()
        {
            var enc = new HL7Encoding();
            Segment PID = new Segment("PID", enc);
            // Creates a new Field
            PID.AddNewField("1", 1);

            // Overwrites the old Field
            PID.AddNewField("2", 1);

            Message message = new Message();
            message.AddNewSegment(PID);
            var str = message.SerializeMessage(false);

            Assert.AreEqual("PID|2\r", str);
        }

        [TestMethod]
        public void GetMSH1Test()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var MSH_1 = message.GetValue("MSH.1");
            Assert.AreEqual("|", MSH_1);
        }

        [TestMethod]
        public void GetAckTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();
            var ack = message.GetACK();

            var MSH_3 = message.GetValue("MSH.3");
            var MSH_4 = message.GetValue("MSH.4");
            var MSH_5 = message.GetValue("MSH.5");
            var MSH_6 = message.GetValue("MSH.6");
            var MSH_3_A = ack.GetValue("MSH.3");
            var MSH_4_A = ack.GetValue("MSH.4");
            var MSH_5_A = ack.GetValue("MSH.5");
            var MSH_6_A = ack.GetValue("MSH.6");

            Assert.AreEqual(MSH_3, MSH_5_A);
            Assert.AreEqual(MSH_4, MSH_6_A);
            Assert.AreEqual(MSH_5, MSH_3_A);
            Assert.AreEqual(MSH_6, MSH_4_A);

            var MSH_10 = message.GetValue("MSH.10");
            var MSH_10_A = ack.GetValue("MSH.10");
            var MSA_1_1 = ack.GetValue("MSA.1");
            var MSA_1_2 = ack.GetValue("MSA.2");

            Assert.AreEqual(MSA_1_1, "AA");
            Assert.AreEqual(MSH_10, MSH_10_A);
            Assert.AreEqual(MSH_10, MSA_1_2);
        }

        [TestMethod]
        public void AddSegmentMSHTest()
        {
            var message = new Message();
            message.AddSegmentMSH("test", "sendingFacility", "test","test", "test", "ADR^A19", "test", "D", "2.5");
        }

        [TestMethod]
        public void GetNackTest()
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();

            var error = "Error message";
            var code = "AR";
            var ack = message.GetNACK(code, error);

            var MSH_3 = message.GetValue("MSH.3");
            var MSH_4 = message.GetValue("MSH.4");
            var MSH_5 = message.GetValue("MSH.5");
            var MSH_6 = message.GetValue("MSH.6");
            var MSH_3_A = ack.GetValue("MSH.3");
            var MSH_4_A = ack.GetValue("MSH.4");
            var MSH_5_A = ack.GetValue("MSH.5");
            var MSH_6_A = ack.GetValue("MSH.6");

            Assert.AreEqual(MSH_3, MSH_5_A);
            Assert.AreEqual(MSH_4, MSH_6_A);
            Assert.AreEqual(MSH_5, MSH_3_A);
            Assert.AreEqual(MSH_6, MSH_4_A);

            var MSH_10 = message.GetValue("MSH.10");
            var MSH_10_A = ack.GetValue("MSH.10");
            var MSA_1_1 = ack.GetValue("MSA.1");
            var MSA_1_2 = ack.GetValue("MSA.2");
            var MSA_1_3 = ack.GetValue("MSA.3");

            Assert.AreEqual(MSH_10, MSH_10_A);
            Assert.AreEqual(MSH_10, MSA_1_2);
            Assert.AreEqual(MSA_1_1, code);
            Assert.AreEqual(MSA_1_3, error);
        }

        [TestMethod]
        public void EmptyAndNullFieldsTest()
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nEVN|A04|20110613083617||\"\"\r\n";

            var message = new Message(sampleMessage);
            var isParsed = message.ParseMessage();
            Assert.IsTrue(isParsed);
            Assert.IsTrue(message.SegmentCount > 0);
            var evn = message.Segments("EVN")[0];
            var expectEmpty = evn.Fields(3).Value;
            Assert.AreEqual(string.Empty, expectEmpty);
            var expectNull = evn.Fields(4).Value;
            Assert.AreEqual(null, expectNull);
        }

        [TestMethod]
        public void MessageWithNullsIsReversable() 
        {
            const string sampleMessage = "MSH|^~\\&|SA|SF|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nEVN|A04|20110613083617||\"\"\r\n";
            var message = new Message(sampleMessage);
            message.ParseMessage();
            var serialized = message.SerializeMessage(false);
            Assert.AreEqual(sampleMessage, serialized);
        }

        [TestMethod]
        public void MessageWithTabsIsReversable() 
        {
            const string sampleMessage = "MSH|^~\\&|Sending\tApplication|Sending\tFacility|RA|RF|20110613083617||ADT^A04|123|P|2.7||||\r\nEVN|A04|20110613083617\r\n";
            var message = new Message(sampleMessage);
            message.ParseMessage();
            var serialized = message.SerializeMessage(false);
            Assert.AreEqual(sampleMessage, serialized);
        }

        [TestMethod]
        public void RemoveSegment() 
        {
            var message = new Message(this.HL7_ADT);
            message.ParseMessage();
            Assert.AreEqual(message.Segments("NK1").Count, 2);
            message.RemoveSegment("NK1", 1);
            Assert.AreEqual(message.Segments("NK1").Count, 1);
            message.RemoveSegment("NK1");
            Assert.AreEqual(message.Segments("NK1").Count, 0);
        }

        [DataTestMethod]
        [DataRow("   20151231234500.1234+2358   ")]
        [DataRow("20151231234500.1234+2358")]
        [DataRow("20151231234500.1234-2358")]
        [DataRow("20151231234500.1234")]
        [DataRow("20151231234500.12")]
        [DataRow("20151231234500")]
        [DataRow("201512312345")]
        [DataRow("2015123123")]
        [DataRow("20151231")]
        [DataRow("201512")]
        [DataRow("2015")]
        public void ParseDateTime_Smoke_Positive(string dateTimeString)
        {
            var date = MessageHelper.ParseDateTime(dateTimeString);
            Assert.IsNotNull(date);
        }

        [DataTestMethod]
        [DataRow("   20151231234500.1234+23581")]
        [DataRow("20151231234500.1234+23")]
        [DataRow("20151231234500.12345")]
        [DataRow("20151231234500.")]
        [DataRow("2015123123450")]
        [DataRow("20151231234")]
        [DataRow("201512312")]
        [DataRow("2015123")]
        [DataRow("20151")]
        [DataRow("201")]
        public void ParseDateTime_Smoke_Negative(string dateTimeString)
        {
            var date = MessageHelper.ParseDateTime(dateTimeString);
            Assert.IsNull(date);
        }

        [TestMethod]
        public void ParseDateTime_Correctness()
        {
            TimeSpan offset;
            var date = MessageHelper.ParseDateTime("20151231234500.1234-2358", out offset).Value;
            // Assert.AreEqual(0, d
            Assert.AreEqual(date, new DateTime(2015,12,31,23,45,00,123));
            Assert.AreEqual(offset, new TimeSpan(-23,58,0));
        }

        [TestMethod]
        public void ParseDateTime_WithException()
        {
            try
            {
                var date = MessageHelper.ParseDateTime("201", true);
                Assert.Fail();
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch
            {
            }
        }

        [TestMethod]
        public void ParseDateTimeOffset_WithException()
        {
            try
            {
                var date = MessageHelper.ParseDateTime("201", out TimeSpan offset, true);
                Assert.Fail();
            }
            catch (AssertFailedException)
            {
                throw;
            }
            catch
            {
            }
        }

        [DataTestMethod]
        [DataRow("PV1.7.1", "1447312459")]
        [DataRow("PV1.7(1).1", "1447312459")]
        [DataRow("PV1.7[1].1", "1447312459")]
        [DataRow("PV1.7(2).1", "DOEM06")]
        [DataRow("PV1.7[2].1", "DOEM06")]
        [DataRow("PV1.7[2].3", "MICHAEL")]
        public void RepetitionTest(string index, string expected)
        {
            var sampleMessage = 
                @"MSH|^~\&|EPIC||||20191107134803|ALEVIB01|ORM^O01|23|T|2.3|||||||||||
PID|1||1005555^^^NYU MRN^MRN||OSTRICH^DODUO||19820605|M||U|000 PARK AVE SOUTH^^NEW YORK^NY^10010^US^^^60|60|(555)555-5555^HOME^PH|||S|||999-99-9999|||U||N||||||||
PV1||O|NWSLED^^^NYULHLI^^^^^LI NW SLEEP DISORDER^^DEPID||||1447312459^DOE^MICHAEL^^^^^^EPIC^^^^PNPI~DOEM06^DOE^MICHAEL^^^^^^KID^^^^KID|1447312459^DOE^MICHAEL^^^^^^EPIC^^^^PNPI~DOEM06^DOE^MICHAEL^^^^^^KID^^^^KID|||||||||||496779945|||||||||||||||||||||||||20191107|||||||V";

            var message = new Message(sampleMessage);
            message.ParseMessage();

            string attendingDrId = message.GetValue(index);
            Assert.AreEqual(expected, attendingDrId);
        }

        private Message ParseHL7(string path)
        {
            try
            {
                var lines = File.ReadLines(path).ToList();

                lines[lines.Count - 1] = lines[lines.Count - 1].Replace(@"\R\", string.Empty);
                lines[lines.Count - 1] = Regex.Replace(lines[lines.Count - 1], @"(\\[0-9]+)", string.Empty);

                foreach (var line in lines.ToList())
                {
                    if (lines.Count - lines.IndexOf(line) == 1)
                    {
                        lines[lines.Count - 1] = lines[lines.Count - 1].Replace(@"\.br", string.Empty);
                        lines[lines.Count - 1] = lines[lines.Count - 1].Replace(@"\X0A", @"\X00A");

                    }
                    else
                    {
                        lines[lines.IndexOf(line)] = lines[lines.IndexOf(line)].Replace(@"\.br\", " ");
                        lines[lines.IndexOf(line)] = lines[lines.IndexOf(line)].Replace(@"\X0A", @"\X00A");
                    }
                }

                lines[lines.Count - 1] = lines[lines.Count - 1].Replace(@"\.nf", string.Empty);
                lines[lines.Count - 1] = lines[lines.Count - 1].Replace(@"\SBLD", string.Empty);
                lines[lines.Count - 1] = lines[lines.Count - 1].Replace(@"\EBLD", string.Empty);
                lines[lines.Count - 1] = lines[lines.Count - 1].Replace(@"\", string.Empty);

                string rawMessage = string.Join("\n", lines);

                var message = new Message(rawMessage);

                if (!message.ParseMessage())
                {
                    string fileName = "";
                    try
                    {
                        fileName = path.Split("\\").Last().Split('.')[0];

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{ex.Message} {ex.StackTrace}");
                    }
                    return null;
                }
                else
                    return message;
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e.Message} {e.StackTrace}");
                return null;
            }

        }
    }
}
