
var dataTable;

$(document).ready(function (){
    loadDataTable();
});

function loadDataTable(){
    dataTable = $('#tblData').DataTable({
        "ajax": {url: '/admin/user/getall'},
        "columns": [
            {data: 'name', width: "15%"},
            {data: 'email', width: "15%"},
            {data: 'phoneNumber', width: "15%"},
            {data: 'company.name', width: "15%"},
            {data: 'role', width: "15%"},
            {data: {id: 'id', lockoutEnd: 'lockoutEnd'}, width: "25%", "render": function (data){
                var today = new Date().getTime();
                var lockout = new Date(data.lockoutEnd).getTime();
                if(lockout > today){
                    return `<div class="text-center">
                                <a onclick="LockUnlock('${data.id}')" class="btn btn-success text-white" style="cursor: pointer; width: 100px;">
                                            <i class="bi bi-unlock-fill"></i> Unlock
                                </a>
                                <a href="http://localhost:5210/Admin/User/RoleManagement/${data.id}" class="btn btn-success text-white" style="cursor: pointer; width: 150px;">
                                            <i class="bi bi-pencil-square"></i> Permission
                                </a>
                            </div>
                 `
                }
                return `<div class="text-center">
                            <a onclick="LockUnlock('${data.id}')"  class="btn btn-danger text-white" style="cursor: pointer; width: 100px;">
                                        <i class="bi bi-lock-fill"></i> Lock
                            </a>
                            <a href="http://localhost:5210/Admin/User/RoleManagement/${data.id}" class="btn btn-danger text-white" style="cursor: pointer; width: 150px;">
                                        <i class="bi bi-pencil-square"></i> Permission
                            </a>
                        </div>
             `
                
                }}
        ]
    });
}

function LockUnlock(id){
    $.ajax(
        {
            type: "POST",
            url: "/Admin/User/LockUnlock/",
            data: JSON.stringify(id),
            contentType: "application/json",
            success: function (data){
                console.log()
                if(data.success){
                    toastr.success(`${data.message}`);
                    console.log("Reloading DataTable");
                    dataTable.ajax.reload(); // Reload without resetting the paging
                } else {
                    toastr.error("Operation failed");
                }
            },
            error: function(err){
                console.error("Error during LockUnlock:", err);
                toastr.error("Error occurred");
            }
        }
    )
}