$(document).ready(function () {
    // console.log('Test');
    $(".button-none").change(function (val) {
        // console.log('value', val);
        // console.log($(this).text());
        var text = $('option:selected', this).text();
        console.log('text new ', text);
        if (text == "Used") {
            $("#remaining-paid-leaves").hide();
            $("#remaining-paid-sick-leaves").hide();
            $("#remaining-paid-carried-leaves").hide();
            $("#Maternity").hide();
            $("#Paternity").hide();
            $("#Caregiver").hide();
            $("#used-leave-paid").show();
            $("#used-leave-sick").show();
            $("#used-leave-carried").show();
            $("#Maternity-used").show();
            $("#Paternity-used").show();
            $("#Caregiver-used").show();
            $("#allow-leave-paid").hide();
            $("#allow-leave-sick").hide();
            $("#allow-leave-carried").hide();
        } else if (text == "Remaining") {
            $("#remaining-paid-leaves").show();
            $("#remaining-paid-sick-leaves").show();
            $("#remaining-paid-carried-leaves").show();
            $("#Maternity").show();
            $("#Paternity").show();
            $("#Caregiver").show();
            $("#used-leave-paid").hide();
            $("#used-leave-sick").hide();
            $("#used-leave-carried").hide();
            $("#Maternity-used").hide();
            $("#Paternity-used").hide();
            $("#Caregiver-used").hide();
            $("#allow-leave-paid").hide();
            $("#allow-leave-sick").hide();
            $("#allow-leave-carried").hide();
        } else {
            $("#allow-leave-paid").show();
            $("#allow-leave-sick").show();
            $("#allow-leave-carried").show();
            $("#remaining-paid-leaves").hide();
            $("#remaining-paid-sick-leaves").hide();
            $("#remaining-paid-carried-leaves").hide();
            $("#used-leave-paid").hide();
            $("#used-leave-sick").hide();
            $("#used-leave-carried").hide();
        }
    });
});