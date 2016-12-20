After regeneration of help documentation replace script in Index.html with this one:

        window.location.replace"html/html/05051a2b-2c6e-4253-8ee0-dff53a879e66.htm");

with this one:

        var base = window.location.href;
        base = base.substr(0, base.lastIndexOf("/") + 1);
        window.location.replace(base + "html/05051a2b-2c6e-4253-8ee0-dff53a879e66.htm");
