$('#tblProducts').DataTable({
    ajax: '/api/products',
    columns: [
        { data: 'title', width: '25%' },
        { data: 'isbn', width: '10%' },
        { data: 'price', width: '5%', render: data => `€${data.toFixed(2)}` },
        { data: 'author', width: '15%' },
        { data: 'category.name', width: '20%', render: data => `<span class="badge bg-primary">${data}</span>` },
        { 
            data: '',
            width: '20%',
            render: (data, type, row) => `
<div class="d-flex gap-2 justify-content-end">
    <a href="/Admin/Product/Update?id=${row.id}" class="btn btn-sm btn-outline-success">
        <i class="bi bi-pencil-square"></i> Edit
    </a>
    <a onclick="deleteProduct(${row.id}, '${row.title}')" class="btn btn-sm btn-outline-danger">
        <i class="bi bi-trash"></i> Delete
    </a>
</div>`
        }]
});

function deleteProduct(id, title) {
    Swal.fire({
        title: `Are you sure you want to delete '${title}'?`,
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!",
        cancelButtonText: "No, cancel!",
    }).then(result => {
        if (result.isConfirmed) {
            $.ajax({
                url: `/api/products/delete?id=${id}`,
                type: 'DELETE',
                success(data) {
                    Swal.fire({
                        title: "Deleted!",
                        text: `${data.data}`,
                        icon: "success"
                    }).then(() => {
                        window.location.reload();
                    });
                }
            });
        }
    });
}
