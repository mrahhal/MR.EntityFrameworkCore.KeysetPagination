list.of.packages <- c("ggplot2", "dplyr", "gdata", "tidyr", "grid", "gridExtra", "Rcpp", "R.devices", "shiny", "ggpubr")
new.packages <- list.of.packages[!(list.of.packages %in% installed.packages()[, "Package"])]
if (length(new.packages)) install.packages(new.packages, lib = Sys.getenv("R_LIBS_USER"), repos = "https://cran.rstudio.com/")

library(ggplot2)
library(dplyr)
library(gdata)
library(tidyr)
library(grid)
library(gridExtra)
library(R.devices)
library(shiny)
library(ggpubr)

info <- textGrob(
    "Benchmarks for MR.EntityFrameworkCore.KeysetPagination @mrahhal",
    gp = gpar(fontface = 3, fontsize = 10),
    x = 1,
    y = .1,
    just = c("right", "bottom")
)

add_info <- function(plot) grid.arrange(plot, bottom = info)

save <- function(filename, plot) ggsave(filename, plot = add_info(plot))

out_dir <- "out"
out_dir_main <- paste(out_dir, "/main", sep = "")
out_dir_opt <- paste(out_dir, "/opt", sep = "")

color_red <- "#f8766d"
color_green <- "#7cae00"
color_green2 <- "#5d7a14"

# Prep the output directories.
if (!dir.exists(out_dir)) {
    dir.create(out_dir)
}
if (!dir.exists(out_dir_main)) {
    dir.create(out_dir_main)
}
if (!dir.exists(out_dir_opt)) {
    dir.create(out_dir_opt)
}

read_measurements <- function(filename) {
    file_to_read <- filename
    if (!file.exists(file_to_read)) {
        file_to_read <- paste("../BenchmarkDotNet.Artifacts/results/", filename, sep = "")
    }
    if (!file.exists(file_to_read)) {
        message("Couldn't find the measurements file '", filename, "' to read.")
        return(NULL)
    }

    message("Reading measurements from file: ", file_to_read)

    read.csv(file_to_read, sep = ",") %>%
        filter(Measurement_IterationStage == "Result") %>%
        subset(
            select = c(
                Target_Method,
                Params,
                Measurement_Value,
                Allocated_Bytes
            )
        ) %>%
        # Convert all measurements to s from ns.
        mutate(Measurement_Value = Measurement_Value / 1000000000) %>%
        rowwise() %>%
        mutate(data.frame(parseQueryString(Params))) %>%
        subset(select = -c(Params))
}

measurements_main <- read_measurements("MainBenchmarks-measurements.csv")
measurements_opt <- read_measurements("FirstColPredicateOptBenchmarks-measurements.csv")

get_N_axis_labels <- function(include_10m) {
    N_axis_labels <- c("1K", "10K", "100K", "1M")
    if (include_10m) {
        N_axis_labels <- append(N_axis_labels, "10M")
    }
    N_axis_labels
}

get_methods <- function(include_additional_keyset_methods) {
    methods <- c(
        "Offset_FirstPage" = color_red,
        "Keyset_FirstPage" = color_green,
        "Offset_MidPage" = color_red,
        "Keyset_MidPage" = color_green,
        "Offset_LastPage" = color_red,
        "Keyset_LastPage" = color_green
    )

    if (include_additional_keyset_methods) {
        methods <- append(methods, c(
            "Keyset_LastPage_Backward" = color_green2,
            "Keyset_SecondToLastPage_Before" = color_green2
        ))
    }

    methods
}

get_result_for <- function(measurements_original, include_10m, methods) {
    method_names <- names(methods)

    measurements <- filter(
        measurements_original,
        Target_Method %in% method_names
    ) %>% filter(Order == order)

    if (!include_10m) {
        measurements <- filter(measurements, N != "10000000")
    }

    # Even though calls to this plot function uses mutually exclusive rows
    # in the data. Assign to a new prop so that the original measurements
    # remain untouched.
    measurements$Measurement <- measurements$Measurement_Value
    time_unit <- "s"
    if (max(measurements$Measurement) < 1) {
        measurements$Measurement <- measurements$Measurement * 1000
        time_unit <- "ms"
    }
    if (max(measurements$Measurement) < 1) {
        measurements$Measurement <- measurements$Measurement * 1000
        time_unit <- "us"
    }
    if (max(measurements$Measurement) < 1) {
        measurements$Measurement <- measurements$Measurement * 1000
        time_unit <- "ns"
    }

    # Reorder groups.
    measurements$Target_Method <- factor(
        measurements$Target_Method,
        levels = method_names
    )

    Ns <- factor(measurements$N)

    list("measurements" = measurements, "time_unit" = time_unit, "Ns" = Ns)
}

plot_main <- function(
    order,
    include_10m = TRUE,
    include_additional_keyset_methods = FALSE) {
    N_axis_labels <- get_N_axis_labels(include_10m)
    methods <- get_methods(include_additional_keyset_methods)

    result <- get_result_for(measurements_main, include_10m, methods)
    measurements <- result$measurements
    time_unit <- result$time_unit

    if (nrow(measurements) > 0) {
        # Main plot combining all methods in one graph.
        p_bar <- ggplot(measurements, aes(x = N, y = Measurement, fill = Target_Method)) +
            geom_bar(position = "dodge", stat = "identity") +
            scale_fill_manual("Method", values = methods) +
            labs(
                title = paste("Order=", order, " (Lower is better)", sep = ""),
                x = "Table records count",
                y = paste("Time (in ", time_unit, ")", sep = "")
            ) +
            scale_x_discrete(labels = N_axis_labels) +
            theme(
                panel.grid.major.x = element_blank(),
                panel.grid.minor.x = element_blank()
            )

        name_includes_10m <- ifelse(include_10m, "-10m", "")
        filename_bar <- paste(out_dir_main, "/benchmark-", order, name_includes_10m, ".png", sep = "")

        save(filename_bar, p_bar)

        # Second plot arranging a few separate method groups in a grid.
        method_groups <- list(
            c("Offset_FirstPage", "Keyset_FirstPage"),
            c("Offset_MidPage", "Keyset_MidPage"),
            c("Offset_LastPage", "Keyset_LastPage")
        )
        plist <- list()
        for (g in method_groups) {
            method_group_measurements <- measurements %>% filter(Target_Method %in% g)
            if (nrow(method_group_measurements) > 0) {
                p <- ggplot(method_group_measurements, aes(x = N, y = Measurement, fill = Target_Method)) +
                    geom_bar(position = "dodge", stat = "identity") +
                    scale_fill_manual("Method", values = methods) +
                    labs(
                        title = paste("Order=", order, " (Lower is better)", sep = ""),
                        x = "Table records count",
                        y = paste("Time (in ", time_unit, ")", sep = "")
                    ) +
                    scale_x_discrete(labels = N_axis_labels) +
                    theme(
                        panel.grid.major.x = element_blank(),
                        panel.grid.minor.x = element_blank()
                    )

                plist <- append(plist, list(p))
            }
        }
        filename_bar <- paste(out_dir_main, "/benchmark-", order, "-grid", name_includes_10m, ".png", sep = "")

        save(filename_bar, grid.arrange(grobs = plist))
    }
}

plot_opt <- function(
    order,
    include_10m = TRUE,
    include_additional_keyset_methods = FALSE) {
    N_axis_labels <- get_N_axis_labels(include_10m)
    methods <- get_methods(include_additional_keyset_methods)

    result <- get_result_for(measurements_opt, include_10m, methods)
    measurements <- result$measurements
    time_unit <- result$time_unit

    name_includes_10m <- ifelse(include_10m, "-10m", "")

    plist <- list()
    for (method in names(methods)) {
        method_measurements <- measurements %>% filter(Target_Method == method)
        if (nrow(method_measurements) > 0) {
            p <- ggplot(method_measurements, aes(x = N, y = Measurement, fill = OptEnabled)) +
                geom_bar(position = "dodge", stat = "identity") +
                scale_fill_manual("OptEnabled", values = c("True" = color_green, "False" = color_red)) +
                labs(
                    title = paste("Order=", order, ", Method=", method, " (Lower is better)", sep = ""),
                    x = "Table records count",
                    y = paste("Time (in ", time_unit, ")", sep = "")
                ) +
                scale_x_discrete(labels = N_axis_labels) +
                theme(
                    panel.grid.major.x = element_blank(),
                    panel.grid.minor.x = element_blank()
                )

            plist <- append(plist, list(p))
        }
    }

    filename_bar <- paste(out_dir_opt, "/benchmark-", order, name_includes_10m, ".png", sep = "")

    save(filename_bar, grid.arrange(grobs = plist))
}

if (!is.null(measurements_main)) {
    orders <- levels(factor(measurements_main$Order))

    for (order in orders) {
        plot_main(order, include_10m = FALSE)
        plot_main(order, include_10m = TRUE)
    }
}

if (!is.null(measurements_opt)) {
    orders <- levels(factor(measurements_opt$Order))

    for (order in orders) {
        plot_opt(order, include_10m = FALSE)
        plot_opt(order, include_10m = TRUE)
    }
}
