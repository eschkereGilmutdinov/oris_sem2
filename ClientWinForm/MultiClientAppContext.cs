using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientWinForm
{
    public class MultiClientAppContext : ApplicationContext
    {
        private int _openForms;

        public MultiClientAppContext(int count)
        {
            for (int i = 1; i <= count; i++)
            {
                var form = new FormRegister(i);
                _openForms++;

                form.FormClosed += (_, __) =>
                {
                    _openForms--;
                    if (_openForms == 0)
                        ExitThread();
                };

                form.Show();
            }
        }
    }
}
