using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

internal class CommandLineException : Exception
{
    public CommandLineException() : base()
    {
    }

    public CommandLineException(string message) : base(message)
    {
    }

    virtual public string GetCommand()
    {
        return "";
    }
}
