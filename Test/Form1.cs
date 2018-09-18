using jsxmpp;
using LitJson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ServiceRequestParam sq = new ServiceRequestParam();
            sq.attributes = new System.Collections.Hashtable();
            sq.attributes.Add("type", "value");
            sq.dataItems = new List<TXDataObject>();
            sq.dataItems.Add(new TXDataObject()
            {
                objectId = "123",
                operateType = "test"
            });
            sq.requestType = "requestType";
            sq.seqId = Guid.NewGuid().ToString();
            sq.source = "source";

            ////测试场景1
            //string strOld = JsonConvert.SerializeObject(sq);
            //ServiceRequestParam jObj = JsonConvert.DeserializeObject<ServiceRequestParam>(strOld);
            //string strNew = JsonConvert.SerializeObject(jObj);
            //MessageBox.Show(strOld + " \n  " + strNew);

            //测试场景2，序列化-->反序列化-->序列化（异常）
            string strOld = JsonMapper.ToJson(sq);
            string str_old = JsonConvert.SerializeObject(sq);
            MessageBox.Show((strOld == str_old).ToString());

            ServiceRequestParam jObj = JsonMapper.ToObject<ServiceRequestParam>(strOld);
            ServiceRequestParam jObj_old = JsonConvert.DeserializeObject<ServiceRequestParam>(strOld);
            string strNew = JsonMapper.ToJson(jObj);
            MessageBox.Show(strOld + " \n  " + strNew);

            ////测试场景3，序列化-->反序列化（old）-->序列化
            //string strOld = JsonMapper.ToJson(sq);
            //ServiceRequestParam jObj = JsonConvert.DeserializeObject<ServiceRequestParam>(strOld);
            //string strNew = JsonMapper.ToJson(jObj);
            //MessageBox.Show(strOld + " \n  " + strNew);
        }
    }
}
