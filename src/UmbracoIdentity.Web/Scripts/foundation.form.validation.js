//Some JS to wire up validation with foundation css nicely
//From: https://gist.github.com/remi/957732

$(document).ready(function () {
    
    function styleValidators(element, result) {
        var validator = $(element).closest("form").find("span[data-valmsg-for='" + $(element).attr("name") + "']");
        if (validator.length > 0) {
            if (result === true) {
                $(element).removeClass("error");
                validator.removeClass("error");
                validator.html("");
            }
            else {
                $(element).addClass("error");
                validator.addClass("error");
            }
        }
    }

    //wires up the events

    $('form.foundationForm').addTriggersToJqueryValidate().triggerElementValidationsOnFormValidation();
    
    $('form.foundationForm input').elementValidation(function (element, result) {
        styleValidators(element, result);
    });

    //on page load, style already existing errors
    $('form.foundationForm span.field-validation-error').each(function () {        
        var element = $(this).closest("form").find("input[name='" + $(this).attr("data-valmsg-for") + "']");
        if (element.length > 0) {
            styleValidators(element, false);
        }        
    });


});