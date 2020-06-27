using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controller
{
    class UI
    {
        private string name;
        private string update;
        private int index;
        private bool flag;
        public UI(string name,string update,int index,bool flag)
        {
            this.name = name;
            this.update = update;
            this.index = index;
            this.flag = flag;
        }

        public UI()
        {

        }

        public string GetName()
        {
            return name;
        }

        public string GetUpdate()
        {
            return update;
        }


        public int GetIndex()
        {
            return index;
        }

        public bool GetFlag()
        {
            return flag;
        }


        public void SetName(string name)
        {
            this.name = name;
        }
        public void SetUpdate(string update)
        {
            this.update = update;
        }

        
        public void SetIndex(int index)
        {
            this.index = index;
        }

        public void SetFlag(bool flag)
        {
            this.flag = flag;
        }
    }
}
