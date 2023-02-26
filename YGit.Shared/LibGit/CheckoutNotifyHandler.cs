using System;
using LibGit2Sharp;

namespace YGit.LibGit
{
    public class CheckoutNotifyHandler
    {
        public void CheckoutNotify(string path, CheckoutNotifyFlags notifyFlags)
        {
            if (notifyFlags.HasFlag(CheckoutNotifyFlags.Conflict))
            {
                Console.WriteLine($"Checkout failed due to conflicts at path: {path}");
            } 
            else if (notifyFlags.HasFlag(CheckoutNotifyFlags.None))
            {
                Console.WriteLine($"Checkout succeeded at path: {path}");
            }
            else if (notifyFlags.HasFlag(CheckoutNotifyFlags.Updated))
            {
                Console.WriteLine($"Checkout updated at path: {path}");
            }
        }
    }
}
