using Stride.Engine;

namespace IKTesting
{
    class IKTestingApp
    {
        static void Main(string[] args)
        {
            using (var game = new Game())
            {
                game.Run();
            }
        }
    }
}
