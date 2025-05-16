abstract class ExpectBaseCommand : Command
{
    public ExpectBaseCommand()
    {
        Input = "-"; // Default to stdin
        Output = null; // Default to stdout
        Append = false;
    }
    
    public string Input { get; set; }
    public string? Output { get; set; }
    public bool Append { get; set; }
    
    public override bool IsEmpty()
    {
        return false;
    }
    
    protected string GetInput()
    {
        if (Input == "-" || string.IsNullOrEmpty(Input))
        {
            return Console.In.ReadToEnd();
        }
        else
        {
            return File.ReadAllText(Input);
        }
    }
    
    protected void WriteOutput(string text)
    {
        if (string.IsNullOrEmpty(Output))
        {
            Console.Write(text);
        }
        else
        {
            if (Append)
            {
                File.AppendAllText(Output, text);
            }
            else
            {
                File.WriteAllText(Output, text);
            }
        }
    }
}