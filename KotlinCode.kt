fun testAllFeatures(limit: Int): Int {
    var count = 0
    
    for (i in 1..limit) {
        if (i > 10) {
            break
        } else {
            count = count + i
        }
    }
    while (count < 100) {
        try {
            count = count + 10
        } catch (e: Exception) {
            return -1
        } finally {
            count = count + 1
        }
    }
    return count
}