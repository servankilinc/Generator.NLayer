function GenerateId() {
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c => (+c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> +c / 4).toString(16));
}


function RenderShortCutsDropdown() {
    let links = localStorage.getItem("list-of-shortcuts");

    $("#dropdownshortcutslist").html("");
    if (links != undefined) {
        let shortcuts = JSON.parse(links);
        if (Array.isArray(shortcuts)) {
            for (let index = 0; index < shortcuts.length;) {
                let firstData = shortcuts[index];
                let secondData = shortcuts[index + 1];
                if (secondData == null) {
                    $("#dropdownshortcutslist").append(`
                        <div class="row row-bordered overflow-visible g-0">
                            <div class="dropdown-shortcuts-item col">
                                <span class="dropdown-shortcuts-icon rounded-circle mb-3">
                                    <i class="${firstData.icon} text-heading"></i>
                                </span>
                                <a href="${firstData.location}" class="stretched-link">${firstData.name}</a>
                            </div>
                        </div>
                    `);
                }
                else {
                    $("#dropdownshortcutslist").append(`
                        <div class="row row-bordered overflow-visible g-0">
                            <div class="dropdown-shortcuts-item col">
                                <span class="dropdown-shortcuts-icon rounded-circle mb-3">
                                    <i class="${firstData.icon} text-heading"></i>
                                </span>
                                <a href="${firstData.location}" class="stretched-link">${firstData.name}</a>
                            </div>
                            <div class="dropdown-shortcuts-item col">
                                <span class="dropdown-shortcuts-icon rounded-circle mb-3">
                                    <i class="${secondData.icon} text-heading"></i>
                                </span>
                                <a href="${secondData.location}" class="stretched-link">${secondData.name}</a>
                            </div>
                        </div>
                    `);
                }

                index += 2;
            }
        }
    }

}

document.addEventListener('DOMContentLoaded', function () {
    // Add New Shortcut
    (function () {
        RenderShortCutsDropdown();
    })();
    if (document.getElementById("dropdown-shortcuts-add") != null)
    {
        document.getElementById("dropdown-shortcuts-add").addEventListener("click", function () {
            let isSubMEnuActive = $("li.menu-item.active").length > 1;
            let activeMenuE = isSubMEnuActive ? $("li .menu-item.active") : $("li.menu-item.active");
            let name = isSubMEnuActive ? `${activeMenuE.closest("ul").parent().find("span.menu-toggle > div.page-name").text()} ${activeMenuE.find("a > div.page-name").text()}` : activeMenuE.find("a > div.page-name").text();
            let icon = activeMenuE.find("a").data("icon");

            let _location = window.location.href;
            let links = localStorage.getItem("list-of-shortcuts");
            let shortcuts = [];
            if (links != undefined) {
                shortcuts = JSON.parse(links);
                let isExist = shortcuts.some(s => s.location == _location);
                if (isExist) {
                    return;
                }
            }

            shortcuts.push({
                name: name,
                icon: icon,
                location: _location
            });
            localStorage.setItem("list-of-shortcuts", JSON.stringify(shortcuts));
            RenderShortCutsDropdown();
        });
    }
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
                $(selec2).select2({
                    placeholder: 'Select an option',
                    allowClear: true,
                    closeOnSelect: true
                }).on('select2:open', function () {
                    document.querySelector('.select2-container--open .select2-search__field').focus();
                });
            });
        }
    }
    else {
        const elemensOfSelect2 = parentElement.find(".autoInitSelect2");

        if (elemensOfSelect2 != null && elemensOfSelect2.length != undefined && elemensOfSelect2.length > 0) {
            elemensOfSelect2.map((index, selec2) => {
                $(selec2).select2({
                    dropdownParent: parentElement,
                    placeholder: 'Select an option',
                    allowClear: true,
                    closeOnSelect: true
                }).on('select2:open', function () {
                    document.querySelector('.select2-container--open .select2-search__field').focus();
                });
            });
        }
    }
}
document.addEventListener('DOMContentLoaded', function () {
    (function () {
        AutoInitSelect2();
    })();
});

/**
 * FlatPicker Auto Initilaze
 */
function AutoInitFlatPicker(parentElement) {

    if (parentElement == undefined) {
        const elemensOfFlatPicker = document.querySelectorAll('.autoInitFlatPicker');

        if (elemensOfFlatPicker != null && elemensOfFlatPicker.length != undefined && elemensOfFlatPicker.length > 0) {
            elemensOfFlatPicker.forEach((flatpickrFriendly) => {
                flatpickrFriendly.flatpickr({
                    altInput: true,
                    altFormat: "F j, Y",
                    dateFormat: "Y-m-d",
                    static: true,
                    todayBtn: true,
                    clearBtn: true,
                    showMonths: true,
                    monthSelectorType: "static"
                });
            });
        }
    }
    else {
        const elemensOfFlatPicker = parentElement.find(".autoInitFlatPicker");

        if (elemensOfFlatPicker != null && elemensOfFlatPicker.length != undefined && elemensOfFlatPicker.length > 0) {
            elemensOfFlatPicker.map((index, flatpickrFriendly) => {
                flatpickrFriendly.flatpickr({
                    altInput: true,
                    altFormat: "F j, Y",
                    dateFormat: "Y-m-d",
                    static: true,
                    todayBtn: true,
                    clearBtn: true,
                    monthSelectorType: "static"
                });
            });
        }
    }
}
document.addEventListener('DOMContentLoaded', function () {
    (function () {
        AutoInitFlatPicker();
    })();
});


/**
 * Datepicker Auto Initilaze
 */
function AutoInitDatePicker(parentElement) {

    if (parentElement == undefined) {
        const elemensOfDatePicker = document.querySelectorAll('.autoInitDatePicker');

        if (elemensOfDatePicker != null && elemensOfDatePicker.length != undefined && elemensOfDatePicker.length > 0) {
            elemensOfDatePicker.forEach((elementOfDatePicker) => {
                $(elementOfDatePicker).datepicker({
                    format: 'dd.mm.yyyy',
                    todayHighlight: true,
                    clearBtn: true,
                    //todayBtn: true,
                    autoclose: true
                });
            });
        }
    }
    else {
        const elemensOfDatePicker = parentElement.find(".autoInitDatePicker");

        var modalId = $(parentElement) != null ? $(parentElement).attr('id') ?? '' : '';
        var parrent = `div#${modalId}`;

        if (elemensOfDatePicker != null && elemensOfDatePicker.length != undefined && elemensOfDatePicker.length > 0) {
            elemensOfDatePicker.map((index, elementOfDatePicker) => {
                $(elementOfDatePicker).datepicker({
                    format: 'dd.mm.yyyy',
                    todayHighlight: true,
                    clearBtn: true,
                    //todayBtn: true,
                    autoclose: true,
                    container: parrent
                });
            });
        }
    }
}
document.addEventListener('DOMContentLoaded', function () {
    (function () {
        AutoInitDatePicker();
    })();
});
 
/**
 * CheckBox Auto Value Setter
 */
document.addEventListener('DOMContentLoaded', function () {
    (function () {
        const elemensOfCheckBoxs = document.querySelectorAll('input[type="checkbox"]');
        
        if (elemensOfCheckBoxs != null) {
            elemensOfCheckBoxs.forEach((checkbox) => {
                checkbox.value = checkbox.checked ? "true" : "false";
                checkbox.addEventListener("change", function () {
                    checkbox.value = checkbox.checked ? "true" : "false";
                });
            });
        }
    })();
});


/**
 * Perfect Scrollbar
 */
document.addEventListener('DOMContentLoaded', function () {
    (function () {
        const elemensOfScroll = document.querySelectorAll('.scrollable-container');

        if (elemensOfScroll != null) {
            elemensOfScroll.forEach((e) => {
                new PerfectScrollbar(e, {
                    wheelPropagation: false
                });
            })
        }
    })();
});



'use strict';

let menu, animate;

(function () {
  // Initialize menu
  //-----------------

  let layoutMenuEl = document.querySelectorAll('#layout-menu');
  layoutMenuEl.forEach(function (element) {
    menu = new Menu(element, {
      orientation: 'vertical',
      closeChildren: false
    });
    // Change parameter to true if you want scroll animation
    window.Helpers.scrollToActive((animate = false));
    window.Helpers.mainMenu = menu;
  });

  // Initialize menu togglers and bind click on each
  let menuToggler = document.querySelectorAll('.layout-menu-toggle');
  menuToggler.forEach(item => {
    item.addEventListener('click', event => {
      event.preventDefault();
      localStorage.setItem("menuCollapseOption", window.Helpers.isCollapsed() ? "expanded":"collapsed");
      window.Helpers.toggleCollapsed();
    });
  });

  // Display menu toggle (layout-menu-toggle) on hover with delay
  let delay = function (elem, callback) {
    let timeout = null;
    elem.onmouseenter = function () {
      // Set timeout to be a timer which will invoke callback after 300ms (not for small screen)
      if (!Helpers.isSmallScreen()) {
        timeout = setTimeout(callback, 300);
      } else {
        timeout = setTimeout(callback, 0);
      }
    };

    elem.onmouseleave = function () {
      // Clear any timers set to timeout
      document.querySelector('.layout-menu-toggle').classList.remove('d-block');
      clearTimeout(timeout);
    };
  };
  if (document.getElementById('layout-menu')) {
    delay(document.getElementById('layout-menu'), function () {
      // not for small screen
      if (!Helpers.isSmallScreen()) {
        document.querySelector('.layout-menu-toggle').classList.add('d-block');
        }
      //else {
      //  localStorage.setItem("menuCollapseOption", "collapsed");
      //  window.Helpers.toggleCollapsed();
      //}
    });
  }

  // Display in main menu when menu scrolls
  let menuInnerContainer = document.getElementsByClassName('menu-inner'),
    menuInnerShadow = document.getElementsByClassName('menu-inner-shadow')[0];
  if (menuInnerContainer.length > 0 && menuInnerShadow) {
    menuInnerContainer[0].addEventListener('ps-scroll-y', function () {
      if (this.querySelector('.ps__thumb-y').offsetTop) {
        menuInnerShadow.style.display = 'block';
      } else {
        menuInnerShadow.style.display = 'none';
      }
    });
  }

  // Init helpers & misc
  // --------------------

  // Init BS Tooltip
  const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
  tooltipTriggerList.map(function (tooltipTriggerEl) {
    return new bootstrap.Tooltip(tooltipTriggerEl);
  });

  // Accordion active class
  const accordionActiveFunction = function (e) {
    if (e.type == 'show.bs.collapse' || e.type == 'show.bs.collapse') {
      e.target.closest('.accordion-item').classList.add('active');
    } else {
      e.target.closest('.accordion-item').classList.remove('active');
    }
  };

  const accordionTriggerList = [].slice.call(document.querySelectorAll('.accordion'));
  const accordionList = accordionTriggerList.map(function (accordionTriggerEl) {
    accordionTriggerEl.addEventListener('show.bs.collapse', accordionActiveFunction);
    accordionTriggerEl.addEventListener('hide.bs.collapse', accordionActiveFunction);
  });

  // Auto update layout based on screen size
  window.Helpers.setAutoUpdate(true);

  // Toggle Password Visibility
  window.Helpers.initPasswordToggle();

  // Speech To Text
  window.Helpers.initSpeechToText();

  // Manage menu expanded/collapsed with templateCustomizer & local storage
  //------------------------------------------------------------------

  // If current layout is horizontal OR current window screen is small (overlay menu) than return from here
  if (window.Helpers.isSmallScreen()) {
    window.Helpers.setCollapsed(true);
    return;
  }

  // If current layout is vertical and current window screen is > small  
    const menuCollapseOption = localStorage.getItem("menuCollapseOption");
    if (window.Helpers.isSmallScreen()) {
        window.Helpers.setCollapsed(true);
    }
    else if (menuCollapseOption != null && menuCollapseOption == "expanded")
    {
        if (window.Helpers.isCollapsed()) {
            window.Helpers.setCollapsed(false);
        }
    }
    else
    {
        window.Helpers.setCollapsed(true);
    }
})();
