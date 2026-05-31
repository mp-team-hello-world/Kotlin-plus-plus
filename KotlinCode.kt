fun main() {
    val score = processData(20)
}

fun processData(limit: Int): Int {
    var result = 0

    for (i in 1..limit) {
        if (i > 10 && limit > 1000) {
            break
        } else {
            result = result + i * i
        }
    }

    while (result < 5000) {
        try {
            result *= 2
        } catch (e: Exception) {
            return -1
        } finally {
            result += 1
        }
    }

    return result
}
