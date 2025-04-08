$('#tblProducts').DataTable({
    ajax: '/api/products',
    columns: [
        { data: 'title', width: '25%' },
        { data: 'isbn', width: '10%' },
        { data: 'price', width: '5%', render: data => `€${data.toFixed(2)}` },
        { data: 'author', width: '15%' },
        { data: 'category.name', width: '20%', render: data => `<span class="badge bg-primary">${data}</span>` },
        { 
            data: 'id',
            width: '20%',
            render: id => `
<div class="d-flex gap-2 justify-content-end">
    <a href="/Product/Update?id=${id}" class="btn btn-sm btn-outline-success">
        <i class="bi bi-pencil-square"></i> Edit
    </a>
    <a href="/Product/Delete?id=${id}" class="btn btn-sm btn-outline-danger">
        <i class="bi bi-trash"></i> Delete
    </a>
</div>`
        }]
});
