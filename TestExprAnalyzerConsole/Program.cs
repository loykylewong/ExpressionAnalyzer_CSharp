
using ExpressionAnalyzer;

ExprAnalyzer ea = new ExprAnalyzer();
ea.Expression = "E*2+3*e-2*sin(a+Pi/(10.02E2-b))/(2.23-c)";
ea.AddVariables(@"E = 2 e=-1,a=0;Pi=3.1415926535897932384626:b=0.996e+3 c=1.23");
double val;
DateTime tp0 = DateTime.Now;
for(int i = 0; i < 1000; i++)
{
   val = ea.Value;
   ea.Invalidate();
}
DateTime tp1 = DateTime.Now;
Console.WriteLine((tp1 - tp0).Milliseconds.ToString() + "ms");

Console.WriteLine("Enter \"var <name1>=<value1>, {<name?>=<value?>}\" for variables.");
Console.WriteLine("      \"eval <expression>\" for evaluate expression.");
string? line;
while(true)
{
    Console.Write("> ");
    line = Console.ReadLine();
    if(line != null)
    {
        if(line.StartsWith("var "))
        {
            ea.AddVariables(line.Substring(4));
        }
        else if(line.StartsWith("eval "))
        {
            ea.Expression = line.Substring(5);
            try
            {
                Console.WriteLine(ea.Value);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        else if(line == "exit")
        {
            break;
        }
        else
        {
            Console.WriteLine("Invalide input.");
            Console.WriteLine("    Enter \"var <name1>=<value1>, {<name?>=<value?>}\" for variables.");
            Console.WriteLine("          \"eval <expression>\" for evaluate expression.");
        }
    }
}