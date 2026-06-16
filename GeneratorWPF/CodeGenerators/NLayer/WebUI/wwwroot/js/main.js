function GenerateId() {
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c => (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16));
}


/**
 * FlatPicker Auto Initilaze
 */
function AutoInitFlatPicker(parentElement) {

    //if (parentElement == undefined)
    {
        const elemensOfFlatPicker = document.querySelectorAll('.autoInitFlatPicker');

        if (elemensOfFlatPicker != null && elemensOfFlatPicker.length != undefined && elemensOfFlatPicker.length > 0) {
            elemensOfFlatPicker.forEach((flatpickrFriendly) => {
                flatpickrFriendly.flatpickr();
            });
        }

        const elemensOfFlatPickerOnlyTime = document.querySelectorAll('.autoInitFlatPickerOnlyTime');

        if (elemensOfFlatPickerOnlyTime != null && elemensOfFlatPickerOnlyTime.length != undefined && elemensOfFlatPickerOnlyTime.length > 0) {
            elemensOfFlatPickerOnlyTime.forEach((flatpickrFriendly) => {
                flatpickrFriendly.flatpickr({
                    enableTime: true,
                    enableTime: true,
                    noCalendar: true,
                    dateFormat: "H:i"
                });
            });
        }
    }
    //else {
    //    const elemensOfFlatPicker = parentElement.find(".autoInitFlatPicker");

    //    if (elemensOfFlatPicker != null && elemensOfFlatPicker.length != undefined && elemensOfFlatPicker.length > 0) {
    //        elemensOfFlatPicker.map((index, flatpickrFriendly) => {
    //            flatpickrFriendly.flatpickr({
    //                altInput: true,
    //                altFormat: "F j, Y",
    //                dateFormat: "Y-m-d",
    //                static: true,
    //                todayBtn: true,
    //                clearBtn: true,
    //                monthSelectorType: "static"
    //            });
    //        });
    //    }
    //}
}
document.addEventListener('DOMContentLoaded', function () {
    (function () {
        AutoInitFlatPicker();
    })();
});



/**
 * Select2 Auto Initilaze
 */
function AutoInitSelect2(parentElement)
{
    if (parentElement == undefined) {
        const elemensOfSelect2 = document.querySelectorAll('.autoInitSelect2');

        if (elemensOfSelect2 != null && elemensOfSelect2.length != undefined && elemensOfSelect2.length > 0) {
            elemensOfSelect2.forEach((selec2) => {
                if ($(selec2).attr('data-select2-id') == null) {
                    $(selec2).select2({
                        placeholder: 'Select an option',
                        allowClear: true,
                        closeOnSelect: true
                    }).on('select2:open', function () {
                        document.querySelector('.select2-container--open .select2-search__field').focus();
                    });
                }
            });
        }
    }
    else {
        const elemensOfSelect2 = parentElement.find(".autoInitSelect2");

        if (elemensOfSelect2 != null && elemensOfSelect2.length != undefined && elemensOfSelect2.length > 0) {
            elemensOfSelect2.map((index, selec2) => {
                if ($(selec2).attr('data-select2-id') == null) {
                    $(selec2).select2({
                        dropdownParent: parentElement,
                        placeholder: 'Select an option',
                        allowClear: true,
                        closeOnSelect: true
                    }).on('select2:open', function () {
                        document.querySelector('.select2-container--open .select2-search__field').focus();
                    });
                }
            });
        }
    }
}
document.addEventListener('DOMContentLoaded', function () {
    (function () {
        AutoInitSelect2();
    })();
});



///**
// * Datepicker Auto Initilaze
// */
//function AutoInitDatePicker(parentElement) {

//    if (parentElement == undefined) {
//        const elemensOfDatePicker = document.querySelectorAll('.autoInitDatePicker');

//        if (elemensOfDatePicker != null && elemensOfDatePicker.length != undefined && elemensOfDatePicker.length > 0) {
//            elemensOfDatePicker.forEach((elementOfDatePicker) => {
//                $(elementOfDatePicker).datepicker({
//                    format: 'dd.mm.yyyy',
//                    todayHighlight: true,
//                    clearBtn: true,
//                    //todayBtn: true,
//                    autoclose: true
//                });
//            });
//        }
//    }
//    else {
//        const elemensOfDatePicker = parentElement.find(".autoInitDatePicker");

//        var modalId = $(parentElement) != null ? $(parentElement).attr('id') ?? '' : '';
//        var parrent = `div#${modalId}`;

//        if (elemensOfDatePicker != null && elemensOfDatePicker.length != undefined && elemensOfDatePicker.length > 0) {
//            elemensOfDatePicker.map((index, elementOfDatePicker) => {
//                $(elementOfDatePicker).datepicker({
//                    format: 'dd.mm.yyyy',
//                    todayHighlight: true,
//                    clearBtn: true,
//                    //todayBtn: true,
//                    autoclose: true,
//                    container: parrent
//                });
//            });
//        }
//    }
//}
//document.addEventListener('DOMContentLoaded', function () {
//    (function () {
//        AutoInitDatePicker();
//    })();
//});
 
///**
// * CheckBox Auto Value Setter
// */
//document.addEventListener('DOMContentLoaded', function () {
//    (function () {
//    	//const elemensOfCheckBoxs = document.querySelectorAll('.form-check-input');
//        const elemensOfCheckBoxs = document.querySelectorAll('input[type="checkbox"]');
        
//        if (elemensOfCheckBoxs != null) {
//            elemensOfCheckBoxs.forEach((checkbox) => {
//                checkbox.value = checkbox.checked ? "true" : "false";
//                checkbox.addEventListener("change", function () {
//                    checkbox.value = checkbox.checked ? "true" : "false";
//                });
//            });
//        }
//    })();
//});


///**
// * Perfect Scrollbar
// */
//document.addEventListener('DOMContentLoaded', function () {
//    (function () {
//        const elemensOfScroll = document.querySelectorAll('.scrollable-container');

//        if (elemensOfScroll != null) {
//            elemensOfScroll.forEach((e) => {
//                new PerfectScrollbar(e, {
//                    wheelPropagation: false
//                });
//            })
//        }
//    })();
//});