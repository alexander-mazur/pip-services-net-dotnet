After regeneration of help documentation replace script in Index.html with this one:

        window.location.replace"html/html/a045e492-38ad-456a-a71e-4761bb70b8e9.htm");

with this one:

        var base = window.location.href;
        base = base.substr(0, base.lastIndexOf("/") + 1);
        window.location.replace(base + "html/a045e492-38ad-456a-a71e-4761bb70b8e9.htm");
