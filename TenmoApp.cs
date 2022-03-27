using System;
using System.Collections.Generic;
using TenmoClient.Models;
using TenmoClient.Services;

namespace TenmoClient
{
    public class TenmoApp
    {
        private readonly TenmoConsoleService console = new TenmoConsoleService();
        private readonly TenmoApiService tenmoApiService;

        public TenmoApp(string apiUrl)
        {
            tenmoApiService = new TenmoApiService(apiUrl);
        }

        public void Run()
        {
            bool keepGoing = true;
            while (keepGoing)
            {
                // The menu changes depending on whether the user is logged in or not
                if (tenmoApiService.IsLoggedIn)
                {
                    keepGoing = RunAuthenticated();
                }
                else // User is not yet logged in
                {
                    keepGoing = RunUnauthenticated();
                }
            }
        }

        private bool RunUnauthenticated()
        {
            console.PrintLoginMenu();
            int menuSelection = console.PromptForInteger("Please choose an option", 0, 2, 1);
            while (true)
            {
                if (menuSelection == 0)
                {
                    return false;   // Exit the main menu loop
                }

                if (menuSelection == 1)
                {
                    // Log in
                    Login();
                    return true;    // Keep the main menu loop going
                }

                if (menuSelection == 2)
                {
                    // Register a new user
                    Register();
                    return true;    // Keep the main menu loop going
                }
                console.PrintError("Invalid selection. Please choose an option.");
                console.Pause();
            }
        }

        private bool RunAuthenticated()
        {
            console.PrintMainMenu(tenmoApiService.Username);
            int menuSelection = console.PromptForInteger("Please choose an option", 0, 6);
            if (menuSelection == 0)
            {
                // Exit the loop
                return false;
            }

            if (menuSelection == 1)
            {
                // View your current balance
                ViewBalance();
            }

            if (menuSelection == 2)
            {
                // View your past transfers
                ViewTransferList();
            }

            if (menuSelection == 3)
            {
                ViewPendingRequests();
            }

            if (menuSelection == 4)
            {
                SendTransfer();
            }

            if (menuSelection == 5)
            {
                // Request TE bucks
                RequestTransfer();
            }

            if (menuSelection == 6)
            {
                // Log out
                tenmoApiService.Logout();
                console.PrintSuccess("You are now logged out");
            }

            return true;    // Keep the main menu loop going
        }

        private void Login()
        {
            LoginUser loginUser = console.PromptForLogin();
            if (loginUser == null)
            {
                return;
            }

            try
            {
                ApiUser user = tenmoApiService.Login(loginUser);
                if (user == null)
                {
                    console.PrintError("Login failed.");
                }
                else
                {
                    console.PrintSuccess("You are now logged in");
                }
            }
            catch (Exception)
            {
                console.PrintError("Login failed.");
            }
            console.Pause();
        }

        private void Register()
        {
            LoginUser registerUser = console.PromptForLogin();
            if (registerUser == null)
            {
                return;
            }
            try
            {
                bool isRegistered = tenmoApiService.Register(registerUser);
                if (isRegistered)
                {
                    console.PrintSuccess("Registration was successful. Please log in.");
                }
                else
                {
                    console.PrintError("Registration was unsuccessful.");
                }
            }
            catch (Exception)
            {
                console.PrintError("Registration was unsuccessful.");
            }
            console.Pause();
        }

        private void ViewBalance()
        {
            try
            {
                Account account = tenmoApiService.ViewBalance();
                if (account != null)
                {
                    console.PrintBalance(account);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            console.Pause();
        }

        private void ViewTransferList()
        {
            console.PrintTransfersHeader();

            try
            {
                int currentUserAccountId = tenmoApiService.GetCurrentUserAccountId();

                List<Transfer> transfers = tenmoApiService.ViewTransferList();

                if (transfers.Count > 0)
                {
                    foreach (Transfer transfer in transfers)
                    {
                        if (transfer.TransferStatus == "Approved")
                        {
                            if (currentUserAccountId == transfer.AccountFrom)
                            {
                                Console.WriteLine($"{transfer.TransferId}         To: {tenmoApiService.GetUsernameByAccountId(transfer.AccountTo)}      ${transfer.Amount} ");
                            }
                            else
                            {
                                Console.WriteLine($"{transfer.TransferId}       From: {tenmoApiService.GetUsernameByAccountId(transfer.AccountFrom)}      ${transfer.Amount} ");
                            }
                        }
                    }
                }
                else
                {
                    Console.WriteLine("You have no transfer history");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            ViewTransferDetails();
        }

        private void ViewTransferDetails()
        {
            Console.WriteLine("");
            int selectedTransferId = console.PromptForInteger("Please enter transfer ID to view details (0 to cancel)");

            if (selectedTransferId == 0)
            {
                RunAuthenticated();
            }

            Transfer selectedTransfer = tenmoApiService.ViewTransfer(selectedTransferId);

            int currentUserAccountId = tenmoApiService.GetCurrentUserAccountId();
            string currentUserName;
            string otherUserName;

            if (selectedTransfer.AccountFrom == currentUserAccountId)
            {
                currentUserName = tenmoApiService.GetUsernameByAccountId(currentUserAccountId);
                otherUserName = tenmoApiService.GetUsernameByAccountId(selectedTransfer.AccountTo);
                console.PrintTransferDetails(selectedTransfer, currentUserName, otherUserName);
            }
            else
            {
                currentUserName = tenmoApiService.GetUsernameByAccountId(currentUserAccountId);
                otherUserName = tenmoApiService.GetUsernameByAccountId(selectedTransfer.AccountFrom);
                console.PrintTransferDetails(selectedTransfer, otherUserName, currentUserName);
            }

            console.Pause();
        }

        private void ViewUserList()
        {
            try
            {
                List<User> users = tenmoApiService.ViewUserList();
                if (users != null)
                {
                    console.PrintUserList(users);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void SendTransfer()
        {
            ViewUserList();

            int accountFrom = tenmoApiService.GetCurrentUserAccountId();
            Console.WriteLine("");
            int userIdToSend = console.PromptForInteger("Id of the user you are sending to[0]");

            if (userIdToSend == 0)
            {
                RunAuthenticated();
            }

            int accountIdToSend = tenmoApiService.GetAccountId(userIdToSend);
            int userAccountId = tenmoApiService.GetCurrentUserAccountId();

            if (accountIdToSend == userAccountId)
            {
                Console.WriteLine("You cannot send money to yourself.");
                console.Pause();
                SendTransfer();
            }

            int accountToSend = tenmoApiService.GetAccountId(userIdToSend);
            decimal amountToSend = console.PromptForDecimal("Enter amount to send");
            Account account = tenmoApiService.ViewBalance();

            if (amountToSend <= 0 || amountToSend > account.Balance)
            {
                Console.WriteLine("Enter a valid amount");
                console.Pause();
                SendTransfer();
            }

            Transfer transfer = new Transfer();

            transfer.AccountFrom = accountFrom;
            transfer.AccountTo = accountToSend;
            transfer.Amount = amountToSend;

            Transfer newTransfer = tenmoApiService.SendTransfer(transfer);


            if (newTransfer == null)
            {
                Console.WriteLine("Insufficient Funds.");
                console.Pause();
            }

            else
            {
                Console.WriteLine("Your transfer is complete!");
                console.Pause();
            }

        }

        private void ViewPendingRequests()
        {
            console.PrintPendingRequestsHeader();

            try
            {
                int currentUserAccountId = tenmoApiService.GetCurrentUserAccountId();

                List<Transfer> transfers = tenmoApiService.ViewTransferList();

                if (transfers.Count > 0)
                {
                    foreach (Transfer transfer in transfers)
                    {
                        if (transfer.TransferStatus == "Pending" && currentUserAccountId == transfer.AccountFrom)
                        {
                            Console.WriteLine($"{transfer.TransferId}         To: {tenmoApiService.GetUsernameByAccountId(transfer.AccountTo)}      ${transfer.Amount} ");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("You have no pending requests");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("");
            ApproveOrRejectTransfer();

        }

        private void ApproveOrRejectTransfer()
        {
            int selectedTransferId = console.PromptForInteger("Please enter transfer ID to approve/reject (0 to cancel)");

            if (selectedTransferId == 0)
            {
                RunAuthenticated();
            }

            Transfer selectedTransfer = tenmoApiService.ViewTransfer(selectedTransferId);

            console.PrintApproveOrRejectTransfer();
            int approveOrReject = console.PromptForInteger("Please choose an option");

            switch (approveOrReject)
            {
                case 1:
                    tenmoApiService.ApproveTransfer(selectedTransfer);
                    break;
                case 2:
                    tenmoApiService.RejectTransfer(selectedTransfer);
                    break;
                case 0:
                    Console.WriteLine("No problem. Let's head back to the main menu");
                    break;
            }

            Console.WriteLine("Action completed.");
            console.Pause();
        }
    
        private void RequestTransfer()
        {
            ViewUserList();

            int accountTo = tenmoApiService.GetCurrentUserAccountId();
            Console.WriteLine("");
            int userId = console.PromptForInteger("Id of the user you are requesting from[0]");
            int accountFrom = tenmoApiService.GetAccountId(userId);
            decimal amountToSend = console.PromptForDecimal("Enter amount to request");

            Transfer transfer = new Transfer();

            transfer.AccountFrom = accountFrom;
            transfer.AccountTo = accountTo;
            transfer.Amount = amountToSend;

            Transfer newTransfer = tenmoApiService.RequestTransfer(transfer);

            if (newTransfer == null)
            {
                Console.WriteLine("Insufficient Funds.");
            }
            else
            {
                Console.WriteLine("Your request has been made");
            }
   
            console.Pause();

        }
    }
}
