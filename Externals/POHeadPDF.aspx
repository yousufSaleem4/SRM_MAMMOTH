<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="POHeadPDF.aspx.cs" Inherits="PlusCP.Externals.POHeadPDF" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>PO Acknowledgment</title>
  <style>
    body { font-family: Arial; background-color: #f5f5f5; }
    .container { width: 800px; margin: 20px auto; background: #fff; padding: 20px; border: 1px solid #ccc; }
    .terms-box { border: 1px solid #ccc; padding: 10px; height: 150px; overflow-y: auto; background: #fafafa; margin-bottom: 10px; }
    .modal { display: none; position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); }
    .modal-content { background: #fff; margin: 100px auto; padding: 20px; width: 500px; border-radius: 4px; }
    .btn { padding: 8px 16px; margin: 4px; border: none; border-radius: 4px; cursor: pointer; }
    .btn-danger { background: #dc3545; color: #fff; }
    .btn[disabled] { background: #aaa; cursor: not-allowed; }
      .btn-success { background: #28a745; color: #fff; }
    /* ✅ Submit button style */
    .btn-submit {
        display: block;            
        margin: 20px auto;         
        background: #28a745;      
        color: #fff;
        font-size: 20px;          
        padding: 14px 32px;       
        border-radius: 6px;
        cursor: pointer;
        transition: background 0.3s ease, transform 0.2s ease;
    }

    .btn-submit:hover {
        background: #218838;       
        /*transform: translateY(-2px);*/ 
    }

    /* Grid styling same as pehle */
    .custom-grid {
        width: 100%;
        border-collapse: collapse;
        font-family: Arial;
        font-size: 14px;
    }
    .custom-grid th {
        background-color: #003366;
        color: #fff;
        padding: 10px;
        text-align: left;
    }
    .custom-grid td {
        padding: 8px 10px;
        border-bottom: 1px solid #ddd;
    }
    .custom-grid tr:nth-child(even) {
        background-color: #f9f9f9;
    }
    .custom-grid tr:hover {
        background-color: #f1f1f1;
    }
    .custom-grid a {
        text-decoration: none;
    }
    .modal-content {
    background: #fff;
    margin: 100px auto;
    padding: 20px;
    width: 500px;
    border-radius: 6px;
    position: relative;
    box-shadow: 0px 5px 15px rgba(0,0,0,0.3);
}

/* Header style */
.modal-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 2px solid #eee;
    margin-bottom: 10px;
    padding-bottom: 5px;
}

.modal-header h3 {
    margin: 0;
    font-size: 20px;
    color: #333;
}

/* Cross button */
.close-btn {
    font-size: 24px;
    font-weight: bold;
    color: #333;
    cursor: pointer;
    transition: color 0.3s ease;
}

.close-btn:hover {
    color: #dc3545; /* Red hover */
}

 .logo-img {
     max-width: 100%;
     height: auto;
     width: 200px; /* Adjust size as needed */
     margin-left:10px;
     margin-top:10px;
 }

</style>

</head>
<body>
    <form id="form1" runat="server">
  <asp:Image 
    ID="imgLogo" 
    runat="server" 
    CssClass="img-fluid logo-img" 
    ImageUrl="~/Content/Images/Collablly.gif" />
        <div class="container-fluid">

         <asp:GridView ID="gvPOData" runat="server" AutoGenerateColumns="False" CssClass="custom-grid" GridLines="None">
    <Columns> 
        <asp:BoundField DataField="PONumber" HeaderText="PO Number" /> 
        <asp:BoundField DataField="Vendor" HeaderText="Vendor Name" />
        <asp:BoundField DataField="VendorEmail" HeaderText="Vendor Email" /> 
        <asp:BoundField DataField="Buyer" HeaderText="Buyer" /> 

    </Columns>
</asp:GridView>


            <br />
         <asp:Button ID="btnSubmit" runat="server" Text="Submit" CssClass="btn-submit" OnClientClick="showModal(); return false;" />


            <!-- Terms Modal -->
           <div id="termsModal" class="modal">
    <div class="modal-content">
        <!-- ✅ Modal Header -->
        <div class="modal-header">
            <h3>Terms and Conditions</h3>
            <span class="close-btn" onclick="closeModal()">&times;</span>
        </div>

        <div class="terms-box">
            By accepting this Purchase Order (PO), the Supplier agrees to the following Terms & Conditions:
            Payment must be made within 30 days of invoice date.<br />
            The company reserves the right to cancel the order if terms are not followed.<br />
            All disputes shall be resolved as per company policy.
        </div>

        <asp:CheckBox ID="chkAccept" runat="server" 
            Text="I accept the Terms & Conditions" 
            AutoPostBack="false" 
            onclick="toggleAccept()" />

        <br /><br />
        <asp:Button ID="btnAccept" runat="server" Text="Accept" CssClass="btn btn-success" Enabled="false" OnClick="btnAccept_Click" />
        <asp:Button  runat="server" value="Reject" Text="Reject"  class="btn btn-danger"  OnClick="btnReject_Click" />
    </div>
</div>


        </div>
    </form>

    <script type="text/javascript">
        function showModal() { document.getElementById("termsModal").style.display = "block"; }
        function closeModal() { document.getElementById("termsModal").style.display = "none"; }
        function toggleAccept() {
            var checkBox = document.getElementById("<%= chkAccept.ClientID %>");
            var acceptBtn = document.getElementById("<%= btnAccept.ClientID %>");
            acceptBtn.disabled = !checkBox.checked;
        }
    </script>
</body>
</html>
