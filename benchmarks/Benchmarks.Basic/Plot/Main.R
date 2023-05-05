list.of.packages <- c("ggplot2", "dplyr", "gdata", "tidyr", "grid", "gridExtra", "Rcpp", "R.devices", "shiny")
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

out_dir <- "out"

offset_color <- "#f8766d"
keyset_color <- "#7cae00"
keyset_color2 <- "#5d7a14"

# Prep the output directory.
if (!dir.exists(out_dir)) {
    # unlink(out_dir, recursive = TRUE)
    dir.create(out_dir)
}

result_original <- read.csv("Benchmarks-measurements.csv", sep = ",") %>%
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
    mutate(Measurement_Value = Measurement_Value / 1000000000)

result_original <- result_original %>%
    rowwise() %>%
    mutate(N = parseQueryString(Params)$N, Order = parseQueryString(Params)$Order) %>%
    subset(select = -c(Params))

info <- textGrob(
    "Benchmarks for MR.EntityFrameworkCore.KeysetPagination @mrahhal",
    gp = gpar(fontface = 3, fontsize = 10),
    x = 1,
    y = .1,
    just = c("right", "bottom")
)

add_info <- function(plot) grid.arrange(plot, bottom = info)

save <- function(filename, plot) ggsave(filename, plot = add_info(plot))

plot <- function(
    order,
    include_10m = TRUE,
    additional_keyset_methods = FALSE) {

    N_axis_labels <- c("1K", "10K", "100K", "1M")

    if (include_10m) {
        N_axis_labels <- append(N_axis_labels, "10M")
    }

    methods <- c(
        "Offset_FirstPage" = offset_color,
        "Keyset_FirstPage" = keyset_color,
        "Offset_MidPage" = offset_color,
        "Keyset_MidPage" = keyset_color,
        "Offset_LastPage" = offset_color,
        "Keyset_LastPage" = keyset_color
    )

    if (additional_keyset_methods) {
        methods <- append(methods, c(
            "Keyset_LastPage_Backward" = keyset_color2,
            "Keyset_SecondToLastPage_Before" = keyset_color2
        ))
    }

    method_names <- names(methods)

    result <- filter(
        result_original,
        Target_Method %in% method_names
    )

    if (!include_10m) {
        result <- result %>% filter(N != "10000000")
    }

    result <- result %>% filter(Order == order)

    time_unit <- "s"
    if (max(result$Measurement_Value) < 1) {
        result$Measurement_Value <- result$Measurement_Value * 1000
        time_unit <- "ms"
    }
    if (max(result$Measurement_Value) < 1) {
        result$Measurement_Value <- result$Measurement_Value * 1000
        time_unit <- "us"
    }
    if (max(result$Measurement_Value) < 1) {
        result$Measurement_Value <- result$Measurement_Value * 1000
        time_unit <- "ns"
    }

    Ns <- factor(result$N)

    # Reorder groups.
    result$Target_Method <- factor(
        result$Target_Method,
        levels = method_names
    )

    p_bar <- ggplot(result, aes(x = N, y = Measurement_Value, fill = Target_Method)) +
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
    filename_bar <- paste(out_dir, "/", "benchmark-", order, name_includes_10m, ".png", sep = "")

    save(filename_bar, p_bar)
}

orders <- levels(factor(result_original$Order))

for (order in orders) {
    plot(order, include_10m = FALSE)
    plot(order, include_10m = TRUE)
}
